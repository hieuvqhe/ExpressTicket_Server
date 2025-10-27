using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateSeatTypeRequest
    {
        /// <summary>
        /// Seat type code (unique per partner)
        /// </summary>
        /// <example>VIP_PREMIUM</example>
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Seat type name
        /// </summary>
        /// <example>Ghế VIP Premium</example>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Surcharge amount
        /// </summary>
        /// <example>150000</example>
        [Required]
        [Range(0, 1000000)]
        public decimal Surcharge { get; set; }

        /// <summary>
        /// Color code (hex)
        /// </summary>
        /// <example>#FFD700</example>
        [Required]
        [StringLength(7)]
        public string Color { get; set; } = "#CCCCCC";

        /// <summary>
        /// Description
        /// </summary>
        /// <example>Ghế VIP cao cấp với không gian thoải mái</example>
        [StringLength(255)]
        public string? Description { get; set; }
    }
}