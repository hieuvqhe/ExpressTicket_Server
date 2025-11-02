using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class CinemaResponse
    {
        public int CinemaId { get; set; }
        public int PartnerId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Code { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalScreens { get; set; }
        public int ActiveScreens { get; set; }
    }
}