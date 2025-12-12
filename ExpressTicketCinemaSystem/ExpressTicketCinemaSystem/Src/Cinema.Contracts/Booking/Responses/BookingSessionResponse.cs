using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class BookingSessionResponse
    {
        public Guid BookingSessionId { get; set; }
        public string State { get; set; } = "DRAFT";
        public int ShowtimeId { get; set; }
        public object Items { get; set; } = new { seats = Array.Empty<object>(), combos = Array.Empty<object>() };
        public object Pricing { get; set; } = new { subtotal = 0, discount = 0, fees = 0, total = 0, currency = "VND" };
        public DateTime ExpiresAt { get; set; }
        public int Version { get; set; } = 1;
    }
}
