using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    public interface IShowtimeSeatEventStream
    {
        // Publish 1 event tới tất cả subscriber của showtime
        Task PublishAsync(SeatEvent ev, CancellationToken ct = default);

        // Đăng ký nhận stream event của 1 showtime
        IAsyncEnumerable<SeatEvent> SubscribeAsync(int showtimeId, CancellationToken ct = default);
    }
}
