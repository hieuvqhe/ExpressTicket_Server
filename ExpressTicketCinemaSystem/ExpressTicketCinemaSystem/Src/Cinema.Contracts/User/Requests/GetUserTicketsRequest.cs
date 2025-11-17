namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    /// <summary>
    /// Request DTO for getting user's tickets with filtering and pagination
    /// </summary>
    public class GetUserTicketsRequest
    {
        /// <summary>
        /// Filter by ticket type: upcoming (sắp chiếu), past (đã chiếu), all (tất cả)
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Page number (default 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default 20)
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

