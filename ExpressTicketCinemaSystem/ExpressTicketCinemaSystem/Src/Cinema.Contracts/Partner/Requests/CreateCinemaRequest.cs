using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateCinemaRequest
    {
        [Required]
        public string CinemaName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? LogoUrl { get; set; }
    }
}