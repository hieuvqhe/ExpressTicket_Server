namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    public class UpdateUserRequest
    {
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
