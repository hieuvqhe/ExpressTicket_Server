using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class ValidateSeatsResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public bool IsValid { get; set; }
        public List<int> CurrentSeatIds { get; set; } = new();
        public List<SeatValidationError> Errors { get; set; } = new();
    }

    public class SeatValidationError
    {
        public string Rule { get; set; } = string.Empty; // "RULE_1" hoặc "RULE_2"
        public string Message { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public List<string> AffectedSeats { get; set; } = new();
    }
}

