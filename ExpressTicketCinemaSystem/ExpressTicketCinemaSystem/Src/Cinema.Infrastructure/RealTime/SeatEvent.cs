using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    public enum SeatEventType
    {
        Locked,
        Released,
        Sold,
        Heartbeat
    }

    // Event nội bộ truyền qua stream (không phải payload SSE)
    public class SeatEvent
    {
        public int ShowtimeId { get; set; }
        public int SeatId { get; set; }
        public SeatEventType Type { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
