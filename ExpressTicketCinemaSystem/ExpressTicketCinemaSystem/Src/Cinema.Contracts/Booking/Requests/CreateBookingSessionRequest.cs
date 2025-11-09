using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class CreateBookingSessionRequest
    {
        [Required, Range(1, int.MaxValue)]
        public int ShowtimeId { get; set; }
       
    }
}
