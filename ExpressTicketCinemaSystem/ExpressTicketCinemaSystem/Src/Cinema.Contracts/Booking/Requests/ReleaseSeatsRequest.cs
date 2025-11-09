using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class ReleaseSeatsRequest
    {
        [Required, MinLength(1)]
        public List<int> SeatIds { get; set; } = new();
    }
}
