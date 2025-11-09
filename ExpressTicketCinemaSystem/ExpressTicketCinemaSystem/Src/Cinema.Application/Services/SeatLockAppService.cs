using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using InfraRT = ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class SeatLockAppService : ISeatLockAppService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly InfraRT.IShowtimeSeatEventStream _eventStream;

        private static readonly TimeSpan SeatLockTtl = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

        public SeatLockAppService(
            CinemaDbCoreContext db,
            InfraRT.IShowtimeSeatEventStream eventStream)
        {
            _db = db;
            _eventStream = eventStream;
        }

        private static (List<int> seats, List<int> combos) ReadItems(string? itemsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemsJson)) return (new(), new());
                using var doc = JsonDocument.Parse(itemsJson);
                var root = doc.RootElement;

                var seats = root.TryGetProperty("seats", out var sEl) && sEl.ValueKind == JsonValueKind.Array
                    ? sEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                var combos = root.TryGetProperty("combos", out var cEl) && cEl.ValueKind == JsonValueKind.Array
                    ? cEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                return (seats, combos);
            }
            catch { return (new(), new()); }
        }

        private static string WriteItems(List<int> seats, List<int> combos)
            => JsonSerializer.Serialize(new { seats, combos });

        // (1) Bắt single-gap cả đầu & đuôi bằng "tường ảo"
        private static bool ViolatesSingleSeatGap(
            IReadOnlyList<int> orderedSeatNumbers,
            HashSet<int> occupiedAfterPick,
            HashSet<int> unsellables)
        {
            if (orderedSeatNumbers.Count == 0) return false;

            int min = orderedSeatNumbers[0];
            int max = orderedSeatNumbers[^1];

            var walls = new SortedSet<int>(occupiedAfterPick);
            walls.UnionWith(unsellables);
            walls.Add(min - 1); // tường ảo bên trái
            walls.Add(max + 1); // tường ảo bên phải

            int? prev = null;
            foreach (var w in walls)
            {
                if (prev.HasValue)
                {
                    int gapLen = w - prev.Value - 1;
                    if (gapLen == 1) return true;
                }
                prev = w;
            }
            return false;
        }

        public async Task<LockSeatsResponse> LockAsync(Guid sessionId, LockSeatsRequest request, CancellationToken ct = default)
        {
            if (request.SeatIds == null || request.SeatIds.Count == 0)
                throw new ValidationException("seatIds", "Danh sách ghế không được rỗng");

            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            var showtimeId = session.ShowtimeId;

            var st = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.ShowtimeId == showtimeId, ct);
            if (st == null) throw new NotFoundException("Showtime không tồn tại");
            var showtimeScreenId = st.ScreenId;

            // (2) Loại trùng trong request và áp giới hạn theo HỢP
            var requestedDistinct = request.SeatIds.Distinct().ToList();

            // Validate seats thuộc đúng screen của showtime
            var seatsRequested = await _db.Seats
                .Where(se => requestedDistinct.Contains(se.SeatId))
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.Status })
                .ToListAsync(ct);

            if (seatsRequested.Count != requestedDistinct.Count)
                throw new ValidationException("seatIds", "Tồn tại ghế không hợp lệ");

            if (seatsRequested.Any(x => x.ScreenId != showtimeScreenId))
                throw new ValidationException("seatIds", "Ghế không thuộc phòng của showtime");

            var (curSeats, curCombos) = ReadItems(session.ItemsJson);

            var unionCount = new HashSet<int>(curSeats);
            foreach (var sid in requestedDistinct) unionCount.Add(sid);
            if (unionCount.Count > 8)
                throw new ValidationException("seatIds", "Tối đa 8 ghế cho mỗi session.");

            // SOLD check
            var soldSeatIds = await _db.Tickets
                .Where(t => t.ShowtimeId == showtimeId
                         && requestedDistinct.Contains(t.SeatId)
                         && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .Distinct()
                .ToListAsync(ct);
            if (soldSeatIds.Count > 0)
                throw new ConflictException("seatIds", $"Các ghế đã bán: {string.Join(",", soldSeatIds)}");

            // LOCKED bởi session khác (còn hiệu lực)
            var conflictLocked = await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                         && requestedDistinct.Contains(l.SeatId)
                         && l.LockedUntil > now
                         && l.LockedBySession != sessionId)
                .Select(l => l.SeatId)
                .Distinct()
                .ToListAsync(ct);
            if (conflictLocked.Count > 0)
                throw new ConflictException("seatIds", $"Ghế đang bị giữ: {string.Join(",", conflictLocked)}");

            // Build rule single-gap
            var rows = seatsRequested.Select(x => x.RowCode).Distinct().ToList();

            var allSeatsInRows = await _db.Seats
                .Where(se => se.ScreenId == showtimeScreenId && rows.Contains(se.RowCode))
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.Status })
                .ToListAsync(ct);

            var unsellableSeatIds = allSeatsInRows
                .Where(s => s.Status == "Blocked")
                .Select(s => s.SeatId)
                .ToHashSet();

            var soldSet = soldSeatIds.ToHashSet();

            var otherLock = await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                         && l.LockedUntil > now
                         && l.LockedBySession != sessionId)
                .Select(l => new { l.SeatId })
                .ToListAsync(ct);
            var otherLockSet = otherLock.Select(x => x.SeatId).ToHashSet();

            var selectedSet = requestedDistinct.ToHashSet();

            var occupiedAfterPick = new HashSet<int>(soldSet);
            occupiedAfterPick.UnionWith(otherLockSet);
            occupiedAfterPick.UnionWith(selectedSet);

            foreach (var row in rows)
            {
                var rowSeats = allSeatsInRows.Where(s => s.RowCode == row)
                                             .OrderBy(s => s.SeatNumber)
                                             .ToList();

                var orderNums = rowSeats.Select(s => s.SeatNumber).ToList();
                var occupiedNums = rowSeats.Where(s => occupiedAfterPick.Contains(s.SeatId))
                                           .Select(s => s.SeatNumber)
                                           .ToHashSet();
                var unsellableNums = rowSeats.Where(s => unsellableSeatIds.Contains(s.SeatId))
                                             .Select(s => s.SeatNumber)
                                             .ToHashSet();

                if (ViolatesSingleSeatGap(orderNums, occupiedNums, unsellableNums))
                    throw new ValidationException("seatIds", $"Không thể để trống 1 ghế ở giữa tại hàng {row}.");
            }

            var lockedUntil = now.Add(SeatLockTtl);

            // (3) Upsert an toàn + bắt DbUpdateException để đảm bảo all-or-nothing dưới PK (ShowtimeId, SeatId)
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Lấy tất cả lock hiện có cho các ghế yêu cầu
                var existingAll = await _db.SeatLocks
                    .Where(l => l.ShowtimeId == showtimeId && requestedDistinct.Contains(l.SeatId))
                    .ToListAsync(ct);

                var existingMap = existingAll.ToDictionary(x => x.SeatId, x => x);

                // Chuẩn bị thay đổi:
                foreach (var sid in requestedDistinct)
                {
                    if (existingMap.TryGetValue(sid, out var row))
                    {
                        // Nếu của mình -> gia hạn
                        if (row.LockedBySession == sessionId)
                        {
                            row.LockedUntil = lockedUntil;
                        }
                        // Nếu hết hạn -> "chiếm" lại cho mình
                        else if (row.LockedUntil <= now)
                        {
                            row.LockedBySession = sessionId;
                            row.LockedUntil = lockedUntil;
                            row.CreatedAt = now;
                        }
                        // Nếu còn hiệu lực & thuộc session khác -> conflict bảo vệ (cực hiếm vì đã check ở trên)
                        else
                        {
                            throw new ConflictException("seatIds", $"Ghế đang bị giữ: {sid}");
                        }
                    }
                    else
                    {
                        // Không có hàng -> thêm mới, nếu race chèn trùng sẽ bị PK bắt và rơi vào DbUpdateException
                        _db.SeatLocks.Add(new SeatLock
                        {
                            ShowtimeId = showtimeId,
                            SeatId = sid,
                            LockedBySession = sessionId,
                            LockedUntil = lockedUntil,
                            CreatedAt = now
                        });
                    }
                }

                // Cập nhật items + TTL session
                foreach (var sid in requestedDistinct)
                    if (!curSeats.Contains(sid)) curSeats.Add(sid);

                session.ItemsJson = WriteItems(curSeats, curCombos);
                session.ExpiresAt = now.Add(SessionTtl);
                session.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Trường hợp race insert vào PK (ShowtimeId, SeatId) – coi như xung đột
                await tx.RollbackAsync(ct);
                throw new ConflictException("seatIds", "Một hoặc nhiều ghế vừa bị giữ bởi phiên khác. Vui lòng thử lại.");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            // Publish SSE cho từng ghế
            foreach (var sid in requestedDistinct)
            {
                await _eventStream.PublishAsync(new InfraRT.SeatEvent
                {
                    ShowtimeId = showtimeId,
                    SeatId = sid,
                    Type = InfraRT.SeatEventType.Locked,
                    LockedUntil = lockedUntil
                }, ct);
            }

            return new LockSeatsResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                LockedSeatIds = requestedDistinct,
                LockedUntil = lockedUntil,
                CurrentSeatIds = ReadItems(session.ItemsJson).seats
            };
        }

        public async Task<ReleaseSeatsResponse> ReleaseAsync(Guid sessionId, ReleaseSeatsRequest request, CancellationToken ct = default)
        {
            if (request.SeatIds == null || request.SeatIds.Count == 0)
                throw new ValidationException("seatIds", "Danh sách ghế không được rỗng");

            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            var showtimeId = session.ShowtimeId;

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var idsDistinct = request.SeatIds.Distinct().ToList();

                var mine = await _db.SeatLocks
                    .Where(l => l.ShowtimeId == showtimeId
                             && idsDistinct.Contains(l.SeatId)
                             && l.LockedBySession == sessionId)
                    .ToListAsync(ct);

                if (mine.Count > 0)
                    _db.SeatLocks.RemoveRange(mine);

                var (curSeats, curCombos) = ReadItems(session.ItemsJson);
                curSeats = curSeats.Where(x => !idsDistinct.Contains(x)).ToList();
                session.ItemsJson = WriteItems(curSeats, curCombos);
                session.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            foreach (var sid in request.SeatIds.Distinct())
            {
                await _eventStream.PublishAsync(new InfraRT.SeatEvent
                {
                    ShowtimeId = showtimeId,
                    SeatId = sid,
                    Type = InfraRT.SeatEventType.Released
                }, ct);
            }

            return new ReleaseSeatsResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                ReleasedSeatIds = request.SeatIds.Distinct().ToList(),
                CurrentSeatIds = ReadItems(session.ItemsJson).seats
            };
        }
        public async Task<ReplaceSeatsResponse> ReplaceAsync(Guid sessionId, ReplaceSeatsRequest request, CancellationToken ct = default)
        {
            if (request.SeatIds == null)
                throw new ValidationException("seatIds", "Danh sách ghế không được rỗng");

            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            var showtimeId = session.ShowtimeId;

            var requestedDistinct = request.SeatIds.Distinct().ToList();
            if (requestedDistinct.Count > 8)
                throw new ValidationException("seatIds", "Tối đa 8 ghế cho mỗi session.");

            var st = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.ShowtimeId == showtimeId, ct);
            if (st == null) throw new NotFoundException("Showtime không tồn tại");
            var screenId = st.ScreenId;

            var seatsRequested = await _db.Seats
                .Where(se => requestedDistinct.Contains(se.SeatId))
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.Status })
                .ToListAsync(ct);

            if (seatsRequested.Count != requestedDistinct.Count)
                throw new ValidationException("seatIds", "Tồn tại ghế không hợp lệ");

            if (seatsRequested.Any(x => x.ScreenId != screenId))
                throw new ValidationException("seatIds", "Ghế không thuộc phòng của showtime");

            var (curSeats, curCombos) = ReadItems(session.ItemsJson);
            var toAdd = requestedDistinct.Except(curSeats).ToList();
            var toRelease = curSeats.Except(requestedDistinct).ToList();

            // ---- Single-gap check theo set mới ----
            var rows = seatsRequested.Select(x => x.RowCode).Distinct().ToList();

            var allSeatsInRows = await _db.Seats
                .Where(se => se.ScreenId == screenId && rows.Contains(se.RowCode))
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.Status })
                .ToListAsync(ct);

            var unsellableSeatIds = allSeatsInRows
                .Where(s => s.Status == "Blocked")
                .Select(s => s.SeatId)
                .ToHashSet();

            // SOLD/LOCKED check chỉ cho toAdd
            var soldSeatIds = await _db.Tickets
                .Where(t => t.ShowtimeId == showtimeId
                         && toAdd.Contains(t.SeatId)
                         && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .Distinct()
                .ToListAsync(ct);
            if (soldSeatIds.Count > 0)
                throw new ConflictException("seatIds", $"Các ghế đã bán: {string.Join(",", soldSeatIds)}");

            var conflictLocked = await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                         && toAdd.Contains(l.SeatId)
                         && l.LockedUntil > now
                         && l.LockedBySession != sessionId)
                .Select(l => l.SeatId)
                .Distinct()
                .ToListAsync(ct);
            if (conflictLocked.Count > 0)
                throw new ConflictException("seatIds", $"Ghế đang bị giữ: {string.Join(",", conflictLocked)}");

            var otherLockSet = (await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                         && l.LockedUntil > now
                         && l.LockedBySession != sessionId)
                .Select(l => l.SeatId).ToListAsync(ct)).ToHashSet();

            var selectedSet = requestedDistinct.ToHashSet();
            var occupiedAfterPick = new HashSet<int>(soldSeatIds);
            occupiedAfterPick.UnionWith(otherLockSet);
            occupiedAfterPick.UnionWith(selectedSet);

            foreach (var row in rows)
            {
                var rowSeats = allSeatsInRows.Where(s => s.RowCode == row)
                                             .OrderBy(s => s.SeatNumber)
                                             .ToList();

                var orderNums = rowSeats.Select(s => s.SeatNumber).ToList();
                var occupiedNums = rowSeats.Where(s => occupiedAfterPick.Contains(s.SeatId))
                                           .Select(s => s.SeatNumber)
                                           .ToHashSet();
                var unsellableNums = rowSeats.Where(s => unsellableSeatIds.Contains(s.SeatId))
                                             .Select(s => s.SeatNumber)
                                             .ToHashSet();

                if (ViolatesSingleSeatGap(orderNums, occupiedNums, unsellableNums))
                    throw new ValidationException("seatIds", $"Không thể để trống 1 ghế ở giữa tại hàng {row}.");
            }

            var lockedUntil = now.Add(SeatLockTtl);

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // 1) Lock toAdd (upsert an toàn dưới PK (showtime, seat))
                if (toAdd.Count > 0)
                {
                    var existing = await _db.SeatLocks
                        .Where(l => l.ShowtimeId == showtimeId && toAdd.Contains(l.SeatId))
                        .ToListAsync(ct);

                    var map = existing.ToDictionary(x => x.SeatId, x => x);
                    foreach (var sid in toAdd)
                    {
                        if (map.TryGetValue(sid, out var row))
                        {
                            if (row.LockedBySession == sessionId || row.LockedUntil <= now)
                            {
                                row.LockedBySession = sessionId;
                                row.LockedUntil = lockedUntil;
                                row.CreatedAt = now;
                            }
                            else
                            {
                                throw new ConflictException("seatIds", $"Ghế đang bị giữ: {sid}");
                            }
                        }
                        else
                        {
                            _db.SeatLocks.Add(new SeatLock
                            {
                                ShowtimeId = showtimeId,
                                SeatId = sid,
                                LockedBySession = sessionId,
                                LockedUntil = lockedUntil,
                                CreatedAt = now
                            });
                        }
                    }
                }

                // 2) Release toRelease thuộc session
                if (toRelease.Count > 0)
                {
                    var mine = await _db.SeatLocks
                        .Where(l => l.ShowtimeId == showtimeId
                                 && toRelease.Contains(l.SeatId)
                                 && l.LockedBySession == sessionId)
                        .ToListAsync(ct);
                    if (mine.Count > 0)
                        _db.SeatLocks.RemoveRange(mine);
                }

                // 3) Cập nhật ItemsJson, TTL (KHÔNG đụng đến version)
                session.ItemsJson = JsonSerializer.Serialize(new { seats = requestedDistinct, combos = ReadItems(session.ItemsJson).combos });
                session.ExpiresAt = now.Add(SessionTtl);
                session.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                throw new ConflictException("seatIds", "Một hoặc nhiều ghế vừa bị giữ bởi phiên khác. Vui lòng thử lại.");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            // SSE
            foreach (var sid in toAdd)
            {
                await _eventStream.PublishAsync(new InfraRT.SeatEvent
                {
                    ShowtimeId = showtimeId,
                    SeatId = sid,
                    Type = InfraRT.SeatEventType.Locked,
                    LockedUntil = lockedUntil
                }, ct);
            }
            foreach (var sid in toRelease)
            {
                await _eventStream.PublishAsync(new InfraRT.SeatEvent
                {
                    ShowtimeId = showtimeId,
                    SeatId = sid,
                    Type = InfraRT.SeatEventType.Released
                }, ct);
            }

            return new ReplaceSeatsResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                CurrentSeatIds = requestedDistinct,
                LockedUntil = requestedDistinct.Count > 0 ? lockedUntil : null
            };
        }
    }
}
