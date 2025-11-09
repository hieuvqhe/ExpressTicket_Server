using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IShowtimeSeatStreamService
    {
        Task<SnapshotPayload> BuildSnapshotAsync(int showtimeId, CancellationToken ct = default);

        /// <summary>
        /// Stream các sự kiện ghế: seat_locked / seat_released / seat_sold.
        /// Nguồn dữ liệu có thể đến từ ISeatLockService hoặc message bus nội bộ.
        /// </summary>
        IAsyncEnumerable<(string eventName, SeatDeltaPayload payload)> StreamSeatEventsAsync(
            int showtimeId, CancellationToken ct = default);
    }
}
