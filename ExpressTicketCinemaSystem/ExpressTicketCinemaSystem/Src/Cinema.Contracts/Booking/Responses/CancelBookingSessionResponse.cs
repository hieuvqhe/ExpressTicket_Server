using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class CancelBookingSessionResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public List<int> ReleasedSeatIds { get; set; } = new();
        public string State { get; set; } = "CANCELED";
    }
}
