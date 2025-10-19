namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class PartnerApprovalResponse
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal?  CommissionRate { get; set; }
        public DateTime ApprovedAt { get; set; }
        public int ApprovedBy { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        // User information
        public int UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
