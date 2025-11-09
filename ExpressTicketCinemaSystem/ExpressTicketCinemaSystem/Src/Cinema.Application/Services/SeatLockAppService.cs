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

        private static bool ViolatesSingleSeatGap(
            IReadOnlyList<int> orderedSeatNumbers,
            HashSet<int> occupiedAfterPick,
            HashSet<int> unsellables)
        {
            if (orderedSeatNumbers.Count == 0) return false;

            int? lastWall = null;
            foreach (var seatNum in orderedSeatNumbers)
            {
                bool isWall = occupiedAfterPick.Contains(seatNum) || unsellables.Contains(seatNum);
                if (isWall)
                {
                    if (lastWall is int w)
                    {
                        int gapLen = seatNum - w - 1;
                        if (gapLen == 1) return true;
                    }
                    lastWall = seatNum;
                }
            }
            int max = orderedSeatNumbers[^1];
            if (lastWall is int lw)
            {
                int tailGap = (max + 1) - lw - 1;
                if (tailGap == 1) return true;
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

            var seatsRequested = await _db.Seats
                .Where(se => request.SeatIds.Contains(se.SeatId))
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.Status })
                .ToListAsync(ct);

            if (seatsRequested.Count != request.SeatIds.Count)
                throw new ValidationException("seatIds", "Tồn tại ghế không hợp lệ");

            if (seatsRequested.Any(x => x.ScreenId != showtimeScreenId))
                throw new ValidationException("seatIds", "Ghế không thuộc phòng của showtime");

            var (curSeats, curCombos) = ReadItems(session.ItemsJson);
            if (curSeats.Count + request.SeatIds.Count > 8)
                throw new ValidationException("seatIds", "Bạn chỉ có thể chọn tối đa 8 ghế mỗi lần đặt.");

            var soldSeatIds = await _db.Tickets
                .Where(t => t.ShowtimeId == showtimeId
                            && request.SeatIds.Contains(t.SeatId)
                            && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .Distinct()
                .ToListAsync(ct);

            if (soldSeatIds.Count > 0)
                throw new ConflictException("seatIds", $"Các ghế đã bán: {string.Join(",", soldSeatIds)}");

            var conflictLocked = await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                            && request.SeatIds.Contains(l.SeatId)
                            && l.LockedUntil > now
                            && l.LockedBySession != sessionId)
                .Select(l => l.SeatId)
                .Distinct()
                .ToListAsync(ct);

            if (conflictLocked.Count > 0)
                throw new ConflictException("seatIds", $"Ghế đang bị giữ: {string.Join(",", conflictLocked)}");

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
                            && rows.Contains(_db.Seats.Where(s => s.SeatId == l.SeatId)
                                                      .Select(s => s.RowCode)
                                                      .FirstOrDefault())
                            && l.LockedUntil > now
                            && l.LockedBySession != sessionId)
                .Select(l => l.SeatId)
                .ToListAsync(ct);
            var otherLockSet = otherLock.ToHashSet();

            var selectedSet = request.SeatIds.ToHashSet();

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

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var existingMine = await _db.SeatLocks
                    .Where(l => l.ShowtimeId == showtimeId
                                && request.SeatIds.Contains(l.SeatId)
                                && l.LockedBySession == sessionId)
                    .ToListAsync(ct);

                var existingSet = existingMine.Select(x => x.SeatId).ToHashSet();

                foreach (var sid in request.SeatIds)
                {
                    if (existingSet.Contains(sid))
                    {
                        var row = existingMine.First(x => x.SeatId == sid);
                        row.LockedUntil = lockedUntil;
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

                foreach (var sid in request.SeatIds)
                    if (!curSeats.Contains(sid)) curSeats.Add(sid);

                session.ItemsJson = WriteItems(curSeats, curCombos);
                session.ExpiresAt = now.Add(SessionTtl);
                session.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            // Publish sự kiện cho SSE
            foreach (var sid in request.SeatIds)
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
                LockedSeatIds = request.SeatIds,
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
                var mine = await _db.SeatLocks
                    .Where(l => l.ShowtimeId == showtimeId
                                && request.SeatIds.Contains(l.SeatId)
                                && l.LockedBySession == sessionId)
                    .ToListAsync(ct);

                if (mine.Count > 0)
                    _db.SeatLocks.RemoveRange(mine);

                var (curSeats, curCombos) = ReadItems(session.ItemsJson);
                curSeats = curSeats.Where(x => !request.SeatIds.Contains(x)).ToList();
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

            // Publish sự kiện cho SSE
            foreach (var sid in request.SeatIds)
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
                ReleasedSeatIds = request.SeatIds,
                CurrentSeatIds = ReadItems(session.ItemsJson).seats
            };
        }
    }
}
