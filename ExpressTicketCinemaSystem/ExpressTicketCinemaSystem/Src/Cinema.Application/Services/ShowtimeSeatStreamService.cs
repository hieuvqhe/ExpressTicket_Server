using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ShowtimeSeatStreamService : IShowtimeSeatStreamService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly ISeatLockService _seatLock; // đã có trong blueprint của bạn

        public ShowtimeSeatStreamService(CinemaDbCoreContext db, ISeatLockService seatLock)
        {
            _db = db;
            _seatLock = seatLock;
        }

        public async Task<SnapshotPayload> BuildSnapshotAsync(int showtimeId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var show = await _db.Showtimes
                .Include(s => s.Screen)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, ct);

            if (show == null) throw new KeyNotFoundException("Không tìm thấy suất chiếu");

            // Tất cả ghế của phòng
            var seats = await _db.Seats.AsNoTracking()
                .Where(se => se.ScreenId == show.ScreenId)
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.SeatTypeId, se.Status })
                .ToListAsync(ct);

            // Lock còn hiệu lực
            var locks = await _db.SeatLocks.AsNoTracking()
                .Where(l => l.ShowtimeId == showtimeId && l.LockedUntil > now)
                .Select(l => new { l.SeatId, l.LockedUntil })
                .ToListAsync(ct);
            var lockMap = locks.ToDictionary(x => x.SeatId, x => x.LockedUntil);

            // Sold
            var sold = await _db.Tickets.AsNoTracking()
                .Where(t => t.ShowtimeId == showtimeId && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .ToListAsync(ct);
            var soldSet = sold.ToHashSet();

            var result = new SnapshotPayload
            {
                ServerTime = now,
                Seats = seats
                    .OrderBy(x => x.RowCode).ThenBy(x => x.SeatNumber)
                    .Select(s =>
                    {
                        var status = s.Status == "Blocked" ? "BLOCKED"
                                   : soldSet.Contains(s.SeatId) ? "SOLD"
                                   : lockMap.ContainsKey(s.SeatId) ? "LOCKED"
                                   : "AVAILABLE";

                        return new SeatCell
                        {
                            SeatId = s.SeatId,
                            RowCode = s.RowCode ?? "",
                            SeatNumber = s.SeatNumber,
                            SeatTypeId = s.SeatTypeId,
                            Status = status,
                            LockedUntil = lockMap.TryGetValue(s.SeatId, out var lu) ? lu : null
                        };
                    })
                    .ToList()
            };

            return result;
        }

        public async IAsyncEnumerable<(string eventName, SeatDeltaPayload payload)> StreamSeatEventsAsync(
            int showtimeId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // Kênh event đến từ SeatLockService (đã có interface trong blueprint của bạn)
            await foreach (var ev in _seatLock.StreamAsync(showtimeId).WithCancellation(ct))
            {
                if (ev == null) continue;
                // Giả sử SeatEvent có: Type {Locked, Released, Sold}, SeatId, LockedUntil
                // Map về eventName + payload SSE
                var payload = new SeatDeltaPayload { SeatId = ev.SeatId, LockedUntil = ev.LockedUntil };

                switch (ev.Type)
                {
                    case SeatEventType.Locked:
                        yield return ("seat_locked", payload);
                        break;
                    case SeatEventType.Released:
                        yield return ("seat_released", new SeatDeltaPayload { SeatId = ev.SeatId });
                        break;
                    case SeatEventType.Sold:
                        yield return ("seat_sold", new SeatDeltaPayload { SeatId = ev.SeatId });
                        break;
                }
            }
        }
    }

    // Giả lập hợp đồng cho SeatEvent từ ISeatLockService (nếu bạn chưa có)
    public enum SeatEventType { Locked, Released, Sold }

    public class SeatEvent
    {
        public SeatEventType Type { get; set; }
        public int SeatId { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    public interface ISeatLockService
    {
        Task<bool> TryLockAsync(string showtimeId, string seatId, string ownerKey, TimeSpan ttl);
        Task ReleaseAsync(string showtimeId, IEnumerable<string> seatIds, string ownerKey);
        IAsyncEnumerable<SeatEvent> StreamAsync(int showtimeId);
    }
}
