using System.Threading;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    /// <summary>
    /// Interface để publish seat events qua SignalR
    /// </summary>
    public interface IShowtimeSeatEventPublisher
    {
        /// <summary>
        /// Publish seat event đến tất cả clients đang subscribe showtime
        /// </summary>
        Task PublishSeatEventAsync(SeatEvent ev, CancellationToken ct = default);
    }
}




















