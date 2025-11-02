using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateSeatLayoutRequest
    {
        /// <summary>
        /// Total number of rows
        /// </summary>
        /// <example>10</example>
        [Required]
        [Range(1, 50)]
        public int TotalRows { get; set; }

        /// <summary>
        /// Total number of columns per row
        /// </summary>
        /// <example>15</example>
        [Required]
        [Range(1, 30)]
        public int TotalColumns { get; set; }

        /// <summary>
        /// List of seats to create
        /// </summary>
        [Required]
        public List<CreateSeatRequest> Seats { get; set; } = new();
    }

    public class CreateSeatRequest
    {
        /// <summary>
        /// Row code (A, B, C...)
        /// </summary>
        /// <example>A</example>
        [Required]
        [StringLength(2)]
        public string Row { get; set; } = string.Empty;

        /// <summary>
        /// Column number (1, 2, 3...)
        /// </summary>
        /// <example>1</example>
        [Required]
        [Range(1, 30)]
        public int Column { get; set; }

        /// <summary>
        /// Seat type ID
        /// </summary>
        /// <example>1</example>
        [Required]
        public int SeatTypeId { get; set; }

        /// <summary>
        /// Seat status
        /// </summary>
        /// <example>Available</example>
        [Required]
        public string Status { get; set; } = "Available";
        /// <summary>
        /// Seat name (optional)
        /// </summary>
        /// <example>Ghế đôi</example>
        public string? SeatName { get; set; } // THÊM DÒNG NÀY
    }
}