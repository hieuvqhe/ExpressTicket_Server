using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class BulkUpdateSeatsRequest
    {
        /// <summary>
        /// List of seat updates
        /// </summary>
        [Required]
        public List<BulkSeatUpdateRequest> SeatUpdates { get; set; } = new();
    }

    public class BulkSeatUpdateRequest
    {
        /// <summary>
        /// Seat ID
        /// </summary>
        /// <example>1</example>
        [Required]
        public int SeatId { get; set; }

        /// <summary>
        /// Seat type ID
        /// </summary>
        /// <example>2</example>
        [Required]
        public int SeatTypeId { get; set; }

        /// <summary>
        /// Seat status
        /// </summary>
        /// <example>Available</example>
        [Required]
        public string Status { get; set; } = "Available";
    }
}