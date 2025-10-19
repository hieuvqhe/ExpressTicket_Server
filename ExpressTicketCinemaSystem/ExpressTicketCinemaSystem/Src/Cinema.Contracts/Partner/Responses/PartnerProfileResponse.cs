namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PartnerProfileResponse
    {
        // User Information
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        // Partner Business Information
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal? CommissionRate { get; set; }

        // Documents
        public string BusinessRegistrationCertificateUrl { get; set; } = string.Empty;
        public string TaxRegistrationCertificateUrl { get; set; } = string.Empty;
        public string IdentityCardUrl { get; set; } = string.Empty;
        public List<string> TheaterPhotosUrls { get; set; } = new();

        // Status & Metadata
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}