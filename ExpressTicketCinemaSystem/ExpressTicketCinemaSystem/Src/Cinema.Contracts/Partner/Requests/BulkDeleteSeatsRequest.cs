using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    /// <summary>
    /// Request for bulk deleting seats
    /// </summary>
    public class BulkDeleteSeatsRequest
    {
        [Required(ErrorMessage = "Danh sách seatIds là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 ghế để xóa")]
        public List<int> SeatIds { get; set; } = new List<int>();
    }
}