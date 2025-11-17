namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class PartnerRegisterRequest
    {
        // User
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Thong tin doanh nghiep
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }

        // URLs
        public string BusinessRegistrationCertificateUrl { get; set; } = string.Empty;
        public string TaxRegistrationCertificateUrl { get; set; } = string.Empty;
        public string IdentityCardUrl { get; set; } = string.Empty;
        public List<string> TheaterPhotosUrls { get; set; } = new();
        public List<string> AdditionalDocumentsUrls { get; set; } = new();
    }
}
