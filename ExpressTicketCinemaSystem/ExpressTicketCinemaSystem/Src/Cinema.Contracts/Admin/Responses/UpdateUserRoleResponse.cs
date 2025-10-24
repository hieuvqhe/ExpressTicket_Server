namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class UpdateUserRoleResponse
    {
        public string Message { get; set; } = string.Empty;
        public UpdateUserRoleResult Result { get; set; } = new();
    }

    public class UpdateUserRoleResult
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
