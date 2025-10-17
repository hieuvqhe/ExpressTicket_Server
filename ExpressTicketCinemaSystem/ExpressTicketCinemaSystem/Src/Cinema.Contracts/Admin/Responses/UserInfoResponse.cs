namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class UserInfoResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int Verify { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserStatsResponse Stats { get; set; } = new();
    }
}
