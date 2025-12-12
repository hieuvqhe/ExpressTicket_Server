namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class PartnerUpdateRequest
    {
        // Thông tin cá nhân
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Thông tin doanh nghiệp
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }

        // URLs (có thể cập nhật)
        public string BusinessRegistrationCertificateUrl { get; set; } = string.Empty;
        public string TaxRegistrationCertificateUrl { get; set; } = string.Empty;
        public string IdentityCardUrl { get; set; } = string.Empty;
        public List<string> TheaterPhotosUrls { get; set; } = new();
        public List<string> AdditionalDocumentsUrls { get; set; } = new();
    }
}