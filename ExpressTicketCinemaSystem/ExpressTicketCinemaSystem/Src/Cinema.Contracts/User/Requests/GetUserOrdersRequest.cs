namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    /// <summary>
    /// Request DTO for getting user's orders with filtering and pagination
    /// </summary>
    public class GetUserOrdersRequest
    {
        /// <summary>
        /// Filter by order status (PAID, PENDING_PAYMENT, CANCELLED, FAILED, etc.)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter orders from this date (ISO format)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter orders to this date (ISO format)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Page number (default 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default 10)
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}

