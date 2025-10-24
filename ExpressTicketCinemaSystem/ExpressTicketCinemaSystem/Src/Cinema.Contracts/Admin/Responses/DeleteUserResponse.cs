namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class DeleteUserResponse
    {
        public string Message { get; set; } = string.Empty;
        public DeleteUserResult Result { get; set; } = new();
    }

    /// <summary>
    /// Delete user result model
    /// </summary>
    public class DeleteUserResult
    {
        public string UserId { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public DateTime? DeactivatedAt { get; set; }
    }
}