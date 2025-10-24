namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses
{
    public class PartnerLoginInfo
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerStatus { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Thêm partner info - có thể null nếu không phải partner
        public PartnerLoginInfo? PartnerInfo { get; set; }
        public string AccountStatus { get; set; } = "Active";
        public bool IsBanned { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}