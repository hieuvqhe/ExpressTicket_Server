namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class BanUserResponse
    {
        public string Message { get; set; } = string.Empty;
        public BanUserResult Result { get; set; } = new();
    }

    public class BanUserResult
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public DateTime? BannedAt { get; set; }
    }
}
