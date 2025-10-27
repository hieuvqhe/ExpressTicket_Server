using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UpdateSeatTypeRequest
    {
        /// <summary>
        /// Seat type name
        /// </summary>
        /// <example>Ghế VIP Premium Updated</example>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Surcharge amount
        /// </summary>
        /// <example>160000</example>
        [Required]
        [Range(0, 1000000)]
        public decimal Surcharge { get; set; }

        /// <summary>
        /// Color code (hex)
        /// </summary>
        /// <example>#FF6B35</example>
        [Required]
        [StringLength(7)]
        public string Color { get; set; } = "#CCCCCC";

        /// <summary>
        /// Description
        /// </summary>
        /// <example>Ghế VIP cao cấp cập nhật mới</example>
        [StringLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        /// <example>true</example>
        [Required]
        public bool Status { get; set; } = true;
    }
}