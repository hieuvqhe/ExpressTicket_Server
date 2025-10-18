namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    /// <summary>
    /// Request model for delete user operation
    /// </summary>
    public class DeleteUserRequest
    {
        /// <summary>
        /// User ID to delete
        /// </summary>
        /// <example>123</example>
        public string UserId { get; set; } = string.Empty;
    }
}