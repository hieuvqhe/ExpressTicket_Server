using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using SeatValidationError = ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses.SeatValidationError;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using InfraRT = ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class SeatLockAppService : ISeatLockAppService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly InfraRT.IShowtimeSeatEventPublisher _eventPublisher;
        private readonly ILogger<SeatLockAppService> _logger;

        private static readonly TimeSpan SeatLockTtl = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

        public SeatLockAppService(
            CinemaDbCoreContext db,
            InfraRT.IShowtimeSeatEventPublisher eventPublisher,
            ILogger<SeatLockAppService> logger)
        {
            _db = db;
            _eventPublisher = eventPublisher;
            _logger = logger;
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

        // Helper: Parse seatName để lấy row và position
        // Ví dụ: "A1" -> ("A", 1), "Z0" -> ("Z", 0, isAisle: true)
        private static (string row, int position, bool isAisle) ParseSeatName(string seatName)
        {
            if (string.IsNullOrWhiteSpace(seatName))
                return ("", 0, false);

            // Ghế Z0 là lối đi (aisle)
            if (seatName.Equals("Z0", StringComparison.OrdinalIgnoreCase))
                return ("Z", 0, true);

            // Parse format: [Row][Number], ví dụ: "A1", "B10", "AA5"
            var match = Regex.Match(seatName, @"^([A-Z]+)(\d+)$");
            if (match.Success)
            {
                var row = match.Groups[1].Value;
                var position = int.Parse(match.Groups[2].Value);
                return (row, position, false);
            }

            return ("", 0, false);
        }

        // Helper: Chia hàng thành các blocks (các nhóm ghế giữa các Z0)
        // Trả về danh sách các blocks, mỗi block là danh sách ghế trong block đó
        // allSeatNamesInRow phải được sắp xếp theo thứ tự thực tế trong hàng (theo seatNumber)
        private static List<List<string>> GetSeatBlocks(List<string> allSeatNamesInRow)
        {
            if (allSeatNamesInRow.Count == 0) return new List<List<string>>();

            // Parse tất cả ghế và giữ nguyên thứ tự trong list (giả sử đã được sắp xếp đúng)
            // Chỉ parse để xác định Z0, không sắp xếp lại
            var seats = allSeatNamesInRow
                .Select(s => new { SeatName = s, Info = ParseSeatName(s) })
                .ToList();

            var blocks = new List<List<string>>();
            var currentBlock = new List<string>();

            foreach (var seat in seats)
            {
                if (seat.Info.isAisle)
                {
                    // Gặp Z0 -> kết thúc block hiện tại và bắt đầu block mới
                    if (currentBlock.Count > 0)
                    {
                        blocks.Add(currentBlock);
                        currentBlock = new List<string>();
                    }
                }
                else
                {
                    // Ghế thường -> thêm vào block hiện tại
                    currentBlock.Add(seat.SeatName);
                }
            }

            // Thêm block cuối cùng nếu có
            if (currentBlock.Count > 0)
            {
                blocks.Add(currentBlock);
            }

            return blocks;
        }

        // Rule 1: Không bỏ trống ghế ở giữa
        private static bool ViolatesNoGapInMiddle(
            List<string> allSeatNamesInRow,
            HashSet<string> selectedSeatNames,
            HashSet<string> occupiedSeatNames,
            HashSet<string> unsellableSeatNames)
        {
            if (allSeatNamesInRow.Count == 0) return false;

            // Lấy tất cả ghế đã occupied (selected + occupied + unsellable)
            var allOccupied = new HashSet<string>(selectedSeatNames);
            allOccupied.UnionWith(occupiedSeatNames);
            allOccupied.UnionWith(unsellableSeatNames);

            // Sắp xếp ghế theo position (bỏ qua Z0)
            var sortedSeats = allSeatNamesInRow
                .Select(s => new { SeatName = s, Info = ParseSeatName(s) })
                .Where(x => !x.Info.isAisle) // Bỏ qua ghế Z0
                .OrderBy(x => x.Info.position)
                .ToList();

            if (sortedSeats.Count < 2) return false;

            // Kiểm tra xem có ghế nào bị bỏ trống giữa 2 ghế đã occupied không
            // Lấy tất cả ghế đã occupied và sắp xếp theo position
            var occupiedSeats = sortedSeats
                .Where(s => allOccupied.Contains(s.SeatName))
                .OrderBy(s => s.Info.position)
                .ToList();

            if (occupiedSeats.Count < 2) return false;

            // Kiểm tra từng cặp ghế đã occupied (không chỉ liền kề)
            for (int i = 0; i < occupiedSeats.Count - 1; i++)
            {
                var current = occupiedSeats[i];
                var next = occupiedSeats[i + 1];

                // Kiểm tra xem có ghế nào ở giữa không
                var gap = next.Info.position - current.Info.position;
                if (gap == 2) // Có đúng 1 ghế ở giữa
                {
                    // Tìm ghế ở giữa
                    var middlePosition = current.Info.position + 1;
                    var middleSeat = sortedSeats.FirstOrDefault(s => s.Info.position == middlePosition);
                    if (middleSeat != null && !allOccupied.Contains(middleSeat.SeatName))
                    {
                        // Có ghế ở giữa chưa được occupied -> vi phạm rule
                        return true;
                    }
                }
            }

            return false;
        }

        // Rule 2: Không được trống lề trái và phải trong mỗi block
        // Rule: Không được book một ghế nếu ghế bên trái nó là lề trái hoặc ghế bên phải nó là lề phải
        // Exception: Nếu số ghế book trong block = số ghế trong block - 1 thì được
        private static bool ViolatesEdgeSeatRule(
            List<string> allSeatNamesInRow,
            HashSet<string> selectedSeatNames,
            HashSet<string> occupiedSeatNames,
            HashSet<string> unsellableSeatNames)
        {
            if (allSeatNamesInRow.Count == 0 || selectedSeatNames.Count == 0) return false;

            // Lấy tất cả ghế đã occupied
            var allOccupied = new HashSet<string>(selectedSeatNames);
            allOccupied.UnionWith(occupiedSeatNames);
            allOccupied.UnionWith(unsellableSeatNames);

            // Chia hàng thành các blocks (giữa các Z0)
            var blocks = GetSeatBlocks(allSeatNamesInRow);
            if (blocks.Count == 0) return false;

            // Kiểm tra từng block
            foreach (var block in blocks)
            {
                if (block.Count == 0) continue;

                // Lề trái: ghế đầu tiên của block
                var leftEdge = block[0];
                // Lề phải: ghế cuối cùng của block
                var rightEdge = block[^1];

                // Lấy danh sách ghế đã selected trong block này
                var selectedInBlock = block
                    .Where(s => selectedSeatNames.Contains(s))
                    .ToList();

                if (selectedInBlock.Count == 0) continue;

                // Exception: Nếu số ghế book trong block = số ghế trong block - 1 thì không áp dụng rule lề
                if (selectedInBlock.Count == block.Count - 1)
                {
                    // Không áp dụng rule lề cho block này
                    continue;
                }

                // Sắp xếp ghế đã selected theo position để tìm range
                var selectedSeats = selectedInBlock
                    .Select(s => new { SeatName = s, Info = ParseSeatName(s) })
                    .OrderBy(x => x.Info.position)
                    .ToList();

                if (selectedSeats.Count == 0) continue;

                // Tìm range của các ghế đã selected (min và max position)
                var minSelectedPosition = selectedSeats[0].Info.position;
                var maxSelectedPosition = selectedSeats[^1].Info.position;

                // Parse lề trái và phải để lấy position
                var leftEdgeInfo = ParseSeatName(leftEdge);
                var rightEdgeInfo = ParseSeatName(rightEdge);

                // Kiểm tra từng ghế đã selected
                foreach (var selectedSeat in selectedSeats)
                {
                    var selectedSeatName = selectedSeat.SeatName;
                    
                    // Tìm index của ghế đã selected trong block
                    var selectedIndex = block.IndexOf(selectedSeatName);
                    if (selectedIndex < 0) continue;

                    // Tìm ghế bên trái ghế này (ghế liền kề bên trái trong block)
                    string? leftNeighborName = selectedIndex > 0 ? block[selectedIndex - 1] : null;

                    // Tìm ghế bên phải ghế này (ghế liền kề bên phải trong block)
                    string? rightNeighborName = selectedIndex < block.Count - 1 ? block[selectedIndex + 1] : null;

                    // Rule: Không được book một ghế nếu ghế bên trái nó là lề trái HOẶC ghế bên phải nó là lề phải
                    // (và lề đó chưa được occupied)
                    
                    // Kiểm tra lề trái: Nếu có ghế bên trái và đó là lề trái
                    if (leftNeighborName != null && leftNeighborName.Equals(leftEdge, StringComparison.OrdinalIgnoreCase))
                    {
                        // Ghế bên trái là lề trái
                        if (!allOccupied.Contains(leftNeighborName))
                        {
                            // Lề trái chưa được occupied -> vi phạm
                            return true;
                        }
                    }

                    // Kiểm tra lề phải: Nếu có ghế bên phải và đó là lề phải
                    if (rightNeighborName != null && rightNeighborName.Equals(rightEdge, StringComparison.OrdinalIgnoreCase))
                    {
                        // Ghế bên phải là lề phải
                        if (!allOccupied.Contains(rightNeighborName))
                        {
                            // Lề phải chưa được occupied -> vi phạm
                            return true;
                        }
                    }
                }
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
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
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

            // Build rule single-gap - cần data trước
            var rows = seatsRequested.Select(x => x.RowCode).Distinct().ToList();
            var allSeatsInRows = await _db.Seats
                .Where(se => se.ScreenId == showtimeScreenId && rows.Contains(se.RowCode))
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
                .ToListAsync(ct);

            var unsellableSeatIds = allSeatsInRows
                .Where(s => s.Status == "Blocked")
                .Select(s => s.SeatId)
                .ToHashSet();

            var lockedUntil = now.Add(SeatLockTtl);

            // ✅ FIX: Move ALL conflict checks và business logic vào TRONG transaction với isolation level
            // Sử dụng Serializable để tránh race condition hoàn toàn
            using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);
            try
            {
                // ✅ Re-check session state trong transaction (optimistic locking)
                await _db.Entry(session).ReloadAsync(ct);
                if (session.State != "DRAFT" || session.ExpiresAt <= now)
                    throw new ValidationException("session", "Session đã hết hạn hoặc không còn hiệu lực");

                // ✅ SOLD check TRONG transaction
                var soldSeatIds = await _db.Tickets
                    .Where(t => t.ShowtimeId == showtimeId
                             && requestedDistinct.Contains(t.SeatId)
                             && (t.Status == "VALID" || t.Status == "USED"))
                    .Select(t => t.SeatId)
                    .Distinct()
                    .ToListAsync(ct);
                if (soldSeatIds.Count > 0)
                    throw new ConflictException("seatIds", $"Các ghế đã bán: {string.Join(",", soldSeatIds)}");

                // ✅ LOCKED check TRONG transaction
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

                // ✅ Single-gap check TRONG transaction với data mới nhất
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

                // Tạo map từ SeatId -> SeatName
                var seatIdToNameMap = allSeatsInRows.ToDictionary(s => s.SeatId, s => 
                    !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}");

                foreach (var row in rows)
                {
                    var rowSeats = allSeatsInRows.Where(s => s.RowCode == row).ToList();
                    var allSeatNamesInRow = rowSeats.Select(s => 
                        !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}")
                        .ToList();

                    // Lấy danh sách ghế đã selected trong hàng này
                    var selectedInRow = rowSeats
                        .Where(s => selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .ToHashSet();

                    // Lấy danh sách ghế đã occupied (sold + locked bởi session khác)
                    var occupiedInRow = rowSeats
                        .Where(s => occupiedAfterPick.Contains(s.SeatId) && !selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .ToHashSet();

                    // ✅ BỎ QUA Rule 1 và Rule 2 validation trong LockAsync
                    // Validation sẽ được thực hiện riêng trong ValidateAsync khi user click "Tiếp tục"
                    // Chỉ check SOLD/LOCKED conflict và max 8 ghế
                }
                // Lấy tất cả lock hiện có cho các ghế yêu cầu (TRONG transaction)
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
                        // Nếu còn hiệu lực & thuộc session khác -> conflict (đã check ở trên nhưng double-check)
                        else
                        {
                            throw new ConflictException("seatIds", $"Ghế đang bị giữ: {sid}");
                        }
                    }
                    else
                    {
                        // Không có hàng -> thêm mới
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

                // ✅ Cập nhật items + TTL session + Version (optimistic locking)
                foreach (var sid in requestedDistinct)
                    if (!curSeats.Contains(sid)) curSeats.Add(sid);

                session.ItemsJson = WriteItems(curSeats, curCombos);
                session.ExpiresAt = now.Add(SessionTtl);
                session.UpdatedAt = now;
                session.Version++; // Tăng version để detect concurrent updates

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // ✅ Optimistic locking conflict
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Optimistic locking conflict for session {SessionId}", sessionId);
                throw new ConflictException("session", "Session đã bị cập nhật bởi request khác. Vui lòng refresh và thử lại.");
            }
            catch (DbUpdateException ex)
            {
                // Trường hợp race insert vào PK (ShowtimeId, SeatId) – coi như xung đột
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Database update conflict for seats {SeatIds} in showtime {ShowtimeId}", 
                    string.Join(",", requestedDistinct), showtimeId);
                throw new ConflictException("seatIds", "Một hoặc nhiều ghế vừa bị giữ bởi phiên khác. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Error locking seats for session {SessionId}", sessionId);
                throw;
            }

            // ✅ Publish SignalR events
            await PublishSeatEventsAsync(requestedDistinct.Select(sid => new InfraRT.SeatEvent
            {
                ShowtimeId = showtimeId,
                SeatId = sid,
                Type = InfraRT.SeatEventType.Locked,
                LockedUntil = lockedUntil
            }).ToList(), ct);

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

            using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);
            try
            {
                // ✅ Re-check session state trong transaction
                await _db.Entry(session).ReloadAsync(ct);
                if (session.State != "DRAFT" || session.ExpiresAt <= now)
                    throw new ValidationException("session", "Session đã hết hạn hoặc không còn hiệu lực");

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
                session.Version++; // Tăng version để detect concurrent updates

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Optimistic locking conflict for session {SessionId}", sessionId);
                throw new ConflictException("session", "Session đã bị cập nhật bởi request khác. Vui lòng refresh và thử lại.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Error releasing seats for session {SessionId}", sessionId);
                throw;
            }

            // ✅ Publish SignalR events
            await PublishSeatEventsAsync(request.SeatIds.Distinct().Select(sid => new InfraRT.SeatEvent
            {
                ShowtimeId = showtimeId,
                SeatId = sid,
                Type = InfraRT.SeatEventType.Released
            }).ToList(), ct);

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
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
                .ToListAsync(ct);

            if (seatsRequested.Count != requestedDistinct.Count)
                throw new ValidationException("seatIds", "Tồn tại ghế không hợp lệ");

            if (seatsRequested.Any(x => x.ScreenId != screenId))
                throw new ValidationException("seatIds", "Ghế không thuộc phòng của showtime");

            var (curSeats, curCombos) = ReadItems(session.ItemsJson);
            var toAdd = requestedDistinct.Except(curSeats).ToList();
            var toRelease = curSeats.Except(requestedDistinct).ToList();

            // Build rule single-gap - cần data trước
            var rows = seatsRequested.Select(x => x.RowCode).Distinct().ToList();
            var allSeatsInRows = await _db.Seats
                .Where(se => se.ScreenId == screenId && rows.Contains(se.RowCode))
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
                .ToListAsync(ct);

            var unsellableSeatIds = allSeatsInRows
                .Where(s => s.Status == "Blocked")
                .Select(s => s.SeatId)
                .ToHashSet();

            var lockedUntil = now.Add(SeatLockTtl);

            // ✅ FIX: Move ALL conflict checks vào TRONG transaction
            using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);
            try
            {
                // ✅ Re-check session state trong transaction
                await _db.Entry(session).ReloadAsync(ct);
                if (session.State != "DRAFT" || session.ExpiresAt <= now)
                    throw new ValidationException("session", "Session đã hết hạn hoặc không còn hiệu lực");
                List<int> soldSeatIds = new();
                // ✅ SOLD/LOCKED check chỉ cho toAdd TRONG transaction
                if (toAdd.Count > 0)
                {
                    soldSeatIds = await _db.Tickets
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
                }
                var otherLockSet = (await _db.SeatLocks
                    .Where(l => l.ShowtimeId == showtimeId
                             && l.LockedUntil > now
                             && l.LockedBySession != sessionId)
                    .Select(l => l.SeatId).ToListAsync(ct)).ToHashSet();

                var selectedSet = requestedDistinct.ToHashSet();
                var occupiedAfterPick = new HashSet<int>(soldSeatIds);
                occupiedAfterPick.UnionWith(otherLockSet);
                occupiedAfterPick.UnionWith(selectedSet);

                // Tạo map từ SeatId -> SeatName
                var seatIdToNameMap = allSeatsInRows.ToDictionary(s => s.SeatId, s => 
                    !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}");

                foreach (var row in rows)
                {
                    var rowSeats = allSeatsInRows.Where(s => s.RowCode == row).ToList();
                    var allSeatNamesInRow = rowSeats.Select(s => 
                        !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}")
                        .ToList();

                    // Lấy danh sách ghế đã selected trong hàng này
                    var selectedInRow = rowSeats
                        .Where(s => selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .ToHashSet();

                    // Lấy danh sách ghế đã occupied (sold + locked bởi session khác)
                    var occupiedInRow = rowSeats
                        .Where(s => occupiedAfterPick.Contains(s.SeatId) && !selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .ToHashSet();

                    // ✅ BỎ QUA Rule 1 và Rule 2 validation trong ReplaceAsync
                    // Validation sẽ được thực hiện riêng trong ValidateAsync khi user click "Tiếp tục"
                    // Chỉ check SOLD/LOCKED conflict và max 8 ghế
                }

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

                // 3) Cập nhật ItemsJson, TTL + Version (optimistic locking)
                session.ItemsJson = JsonSerializer.Serialize(new { seats = requestedDistinct, combos = ReadItems(session.ItemsJson).combos });
                session.ExpiresAt = now.Add(SessionTtl);
                session.UpdatedAt = now;
                session.Version++; // Tăng version để detect concurrent updates

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Optimistic locking conflict for session {SessionId}", sessionId);
                throw new ConflictException("session", "Session đã bị cập nhật bởi request khác. Vui lòng refresh và thử lại.");
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Database update conflict for seats in showtime {ShowtimeId}", showtimeId);
                throw new ConflictException("seatIds", "Một hoặc nhiều ghế vừa bị giữ bởi phiên khác. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Error replacing seats for session {SessionId}", sessionId);
                throw;
            }

            // ✅ Publish SignalR events
            var eventsToPublish = new List<InfraRT.SeatEvent>();
            eventsToPublish.AddRange(toAdd.Select(sid => new InfraRT.SeatEvent
            {
                ShowtimeId = showtimeId,
                SeatId = sid,
                Type = InfraRT.SeatEventType.Locked,
                LockedUntil = lockedUntil
            }));
            eventsToPublish.AddRange(toRelease.Select(sid => new InfraRT.SeatEvent
            {
                ShowtimeId = showtimeId,
                SeatId = sid,
                Type = InfraRT.SeatEventType.Released
            }));
            await PublishSeatEventsAsync(eventsToPublish, ct);

            return new ReplaceSeatsResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                CurrentSeatIds = requestedDistinct,
                LockedUntil = requestedDistinct.Count > 0 ? lockedUntil : null
            };
        }

        public async Task<ValidateSeatsResponse> ValidateAsync(Guid sessionId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            var showtimeId = session.ShowtimeId;
            var st = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.ShowtimeId == showtimeId, ct);
            if (st == null) throw new NotFoundException("Showtime không tồn tại");
            var showtimeScreenId = st.ScreenId;

            // Lấy tất cả ghế đã lock trong session
            var (curSeats, curCombos) = ReadItems(session.ItemsJson);
            if (curSeats.Count == 0)
            {
                return new ValidateSeatsResponse
                {
                    BookingSessionId = sessionId,
                    ShowtimeId = showtimeId,
                    IsValid = true,
                    CurrentSeatIds = new List<int>()
                };
            }

            // Lấy thông tin ghế đã lock
            var lockedSeats = await _db.Seats
                .Where(se => curSeats.Contains(se.SeatId))
                .Select(se => new { se.SeatId, se.ScreenId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
                .ToListAsync(ct);

            if (lockedSeats.Any(x => x.ScreenId != showtimeScreenId))
                throw new ValidationException("seats", "Có ghế không thuộc phòng của showtime");

            // Lấy tất cả ghế trong các hàng có ghế đã lock
            var rows = lockedSeats.Select(x => x.RowCode).Distinct().ToList();
            var allSeatsInRows = await _db.Seats
                .Where(se => se.ScreenId == showtimeScreenId && rows.Contains(se.RowCode))
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.SeatName, se.Status })
                .ToListAsync(ct);

            var unsellableSeatIds = allSeatsInRows
                .Where(s => s.Status == "Blocked")
                .Select(s => s.SeatId)
                .ToHashSet();

            // Lấy ghế đã sold và locked bởi session khác
            var soldSeatIds = await _db.Tickets
                .Where(t => t.ShowtimeId == showtimeId
                         && curSeats.Contains(t.SeatId)
                         && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .Distinct()
                .ToListAsync(ct);

            var otherLock = await _db.SeatLocks
                .Where(l => l.ShowtimeId == showtimeId
                         && l.LockedUntil > now
                         && l.LockedBySession != sessionId)
                .Select(l => new { l.SeatId })
                .ToListAsync(ct);
            var otherLockSet = otherLock.Select(x => x.SeatId).ToHashSet();

            var selectedSet = curSeats.ToHashSet();
            var occupiedAfterPick = new HashSet<int>(soldSeatIds);
            occupiedAfterPick.UnionWith(otherLockSet);
            occupiedAfterPick.UnionWith(selectedSet);

            // Tạo map từ SeatId -> SeatName
            var seatIdToNameMap = allSeatsInRows.ToDictionary(s => s.SeatId, s => 
                !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}");

            var errors = new List<SeatValidationError>();

            // Validate từng hàng
            foreach (var row in rows)
            {
                var rowSeats = allSeatsInRows
                    .Where(s => s.RowCode == row)
                    .OrderBy(s => s.SeatNumber) // Sắp xếp theo seatNumber để đảm bảo thứ tự đúng
                    .ToList();
                var allSeatNamesInRow = rowSeats.Select(s => 
                    !string.IsNullOrWhiteSpace(s.SeatName) ? s.SeatName : $"{s.RowCode}{s.SeatNumber}")
                    .ToList();

                // Lấy danh sách ghế đã selected trong hàng này
                var selectedInRow = rowSeats
                    .Where(s => selectedSet.Contains(s.SeatId))
                    .Select(s => seatIdToNameMap[s.SeatId])
                    .ToHashSet();

                // Lấy danh sách ghế đã occupied (sold + locked bởi session khác)
                var occupiedInRow = rowSeats
                    .Where(s => occupiedAfterPick.Contains(s.SeatId) && !selectedSet.Contains(s.SeatId))
                    .Select(s => seatIdToNameMap[s.SeatId])
                    .ToHashSet();

                // Lấy danh sách ghế unsellable (blocked)
                var unsellableInRow = rowSeats
                    .Where(s => unsellableSeatIds.Contains(s.SeatId))
                    .Select(s => seatIdToNameMap[s.SeatId])
                    .ToHashSet();

                // Rule 1: Không bỏ trống ghế ở giữa
                if (ViolatesNoGapInMiddle(allSeatNamesInRow, selectedInRow, occupiedInRow, unsellableInRow))
                {
                    var selectedSeats = rowSeats
                        .Where(s => selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .OrderBy(s => ParseSeatName(s).position)
                        .ToList();

                    errors.Add(new SeatValidationError
                    {
                        Rule = "RULE_1",
                        Message = $"Không thể để trống 1 ghế ở giữa tại hàng {row}.",
                        Row = row,
                        AffectedSeats = selectedSeats
                    });
                }

                // Rule 2: Không được trống lề trái và phải
                if (ViolatesEdgeSeatRule(allSeatNamesInRow, selectedInRow, occupiedInRow, unsellableInRow))
                {
                    var selectedSeats = rowSeats
                        .Where(s => selectedSet.Contains(s.SeatId))
                        .Select(s => seatIdToNameMap[s.SeatId])
                        .OrderBy(s => ParseSeatName(s).position)
                        .ToList();

                    errors.Add(new SeatValidationError
                    {
                        Rule = "RULE_2",
                        Message = $"Không được để trống ghế ở lề trái hoặc lề phải tại hàng {row}.",
                        Row = row,
                        AffectedSeats = selectedSeats
                    });
                }
            }

            // Tạo message từ errors (lấy message đầu tiên hoặc tổng hợp)
            string message;
            if (errors.Count == 0)
            {
                message = "Tất cả ghế hợp lệ";
            }
            else if (errors.Count == 1)
            {
                // Nếu chỉ có 1 lỗi, dùng message của lỗi đó
                message = errors[0].Message;
            }
            else
            {
                // Nếu có nhiều lỗi, tổng hợp message (loại bỏ trùng lặp)
                var uniqueMessages = errors
                    .Select(e => e.Message)
                    .Distinct()
                    .ToList();
                
                if (uniqueMessages.Count == 1)
                {
                    // Tất cả lỗi có cùng message
                    message = uniqueMessages[0];
                }
                else
                {
                    // Có nhiều message khác nhau, tổng hợp
                    message = string.Join(" ", uniqueMessages);
                }
            }

            return new ValidateSeatsResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                IsValid = errors.Count == 0,
                CurrentSeatIds = curSeats,
                Errors = errors,
                Message = message
            };
        }

        /// <summary>
        /// ✅ Helper: Publish SignalR events
        /// </summary>
        private async Task PublishSeatEventsAsync(
            List<InfraRT.SeatEvent> events,
            CancellationToken ct = default)
        {
            if (events == null || events.Count == 0) return;

            foreach (var evt in events)
            {
                try
                {
                    await _eventPublisher.PublishSeatEventAsync(evt, ct);
                }
                catch (Exception ex)
                {
                    // ✅ Log nhưng không throw - data đã commit, chỉ là SignalR notification fail
                    // Client sẽ nhận update khi refresh hoặc reconnect SignalR
                    _logger.LogWarning(ex,
                        "Failed to publish SignalR event. ShowtimeId: {ShowtimeId}, SeatId: {SeatId}, Type: {Type}",
                        evt.ShowtimeId, evt.SeatId, evt.Type);
                }
            }
        }
    }
}
