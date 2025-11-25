using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    /// <summary>
    /// Implementation publish seat events qua SignalR Hub
    /// </summary>
    public class SignalRShowtimeSeatEventPublisher : IShowtimeSeatEventPublisher
    {
        private readonly IHubContext<ShowtimeSeatHub> _hubContext;

        public SignalRShowtimeSeatEventPublisher(IHubContext<ShowtimeSeatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishSeatEventAsync(SeatEvent ev, CancellationToken ct = default)
        {
            var groupName = $"showtime_{ev.ShowtimeId}";
            
            // Map SeatEvent sang SignalR message
            object payload;
            string eventName;

            switch (ev.Type)
            {
                case SeatEventType.Locked:
                    eventName = "SeatLocked";
                    payload = new SeatDeltaPayload
                    {
                        SeatId = ev.SeatId,
                        LockedUntil = ev.LockedUntil
                    };
                    break;

                case SeatEventType.Released:
                    eventName = "SeatReleased";
                    payload = new SeatDeltaPayload
                    {
                        SeatId = ev.SeatId,
                        LockedUntil = null
                    };
                    break;

                case SeatEventType.Sold:
                    eventName = "SeatSold";
                    payload = new SeatDeltaPayload
                    {
                        SeatId = ev.SeatId,
                        LockedUntil = null
                    };
                    break;

                default:
                    // Unknown event type, skip
                    return;
            }

            // Gửi event đến tất cả clients trong group của showtime
            await _hubContext.Clients.Group(groupName).SendAsync(eventName, payload, ct);
        }
    }
}















