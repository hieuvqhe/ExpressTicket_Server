namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = "********";
        public string AvatarUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; 
    }
}
