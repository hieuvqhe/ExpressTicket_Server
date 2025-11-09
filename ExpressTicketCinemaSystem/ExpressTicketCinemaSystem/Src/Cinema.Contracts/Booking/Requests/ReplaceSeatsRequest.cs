using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class ReplaceSeatsRequest
    {
        /// <summary>
        /// Danh sách ghế muốn thay thế toàn bộ selection hiện tại.
        /// </summary>
        [Required]
        public List<int> SeatIds { get; set; } = new();

    }
}
