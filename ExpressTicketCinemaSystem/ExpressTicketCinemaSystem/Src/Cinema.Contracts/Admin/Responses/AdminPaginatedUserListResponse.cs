namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class AdminPaginatedUserListResponse
    {
        public string Message { get; set; } = string.Empty;
        public AdminPaginatedUserResponse Result { get; set; } = new();
    }
}
