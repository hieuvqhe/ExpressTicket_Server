using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UpdateSeatRequest
    {
        /// <summary>
        /// Seat type ID
        /// </summary>
        /// <example>2</example>
        [Required]
        public int SeatTypeId { get; set; }

        /// <summary>
        /// Seat status
        /// </summary>
        /// <example>Blocked</example>
        [Required]
        public string Status { get; set; } = "Available";
    }
}