namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class PartnerRejectionResponse
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime RejectedAt { get; set; }
        public int RejectedBy { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        // User information
        public int UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
