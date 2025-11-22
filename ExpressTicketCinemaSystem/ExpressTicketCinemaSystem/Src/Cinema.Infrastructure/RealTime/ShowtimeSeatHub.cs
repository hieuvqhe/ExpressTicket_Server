using Microsoft.AspNetCore.SignalR;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    /// <summary>
    /// SignalR Hub để stream realtime trạng thái ghế cho showtime
    /// </summary>
    public class ShowtimeSeatHub : Hub
    {
        /// <summary>
        /// Client join vào group của showtime để nhận events
        /// </summary>
        public async Task JoinShowtime(int showtimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"showtime_{showtimeId}");
        }

        /// <summary>
        /// Client rời khỏi group của showtime
        /// </summary>
        public async Task LeaveShowtime(int showtimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"showtime_{showtimeId}");
        }
    }
}

