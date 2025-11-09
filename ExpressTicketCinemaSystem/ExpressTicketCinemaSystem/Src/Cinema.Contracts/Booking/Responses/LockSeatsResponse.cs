using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class LockSeatsResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public List<int> LockedSeatIds { get; set; } = new();
        public DateTime LockedUntil { get; set; }
        public List<int> CurrentSeatIds { get; set; } = new();
    }
}
