using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class PartnerPendingResponse
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal? CommissionRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User information
        public int UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;

        // Document URLs
        public string? BusinessRegistrationCertificateUrl { get; set; }
        public string? TaxRegistrationCertificateUrl { get; set; }
        public string? IdentityCardUrl { get; set; }
        public string? TheaterPhotosUrl { get; set; }
        public string? AdditionalDocumentsUrl { get; set; }

        // ManagerStaff assignment information
        public int? ManagerStaffId { get; set; }
        public string? ManagerStaffName { get; set; }
        public string? ManagerStaffEmail { get; set; }
        public bool IsAssignedToStaff { get; set; } // Helper property để FE dễ check
    }

    public class PaginatedPartnersResponse
    {
        public List<PartnerPendingResponse> Partners { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}