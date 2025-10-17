namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class PaginatedUserListResponse
    {
        public string Message { get; set; } = string.Empty;
        public PaginatedUserResponse Result { get; set; } = new();
    }
}
