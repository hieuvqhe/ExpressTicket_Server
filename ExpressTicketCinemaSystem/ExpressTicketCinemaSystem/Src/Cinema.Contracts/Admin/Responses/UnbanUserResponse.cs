namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class UnbanUserResponse
    {
        public string Message { get; set; } = string.Empty;
        public UnbanUserResult Result { get; set; } = new();
    }

    public class UnbanUserResult
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public DateTime? UnbannedAt { get; set; }
    }
}
