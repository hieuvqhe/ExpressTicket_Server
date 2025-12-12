namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    /// <summary>
    /// Request DTO for getting customers with successful bookings (Manager only)
    /// </summary>
    public class GetSuccessfulBookingCustomersRequest
    {
        /// <summary>
        /// Top N customers to return for each sort type (default: 5, max: 50)
        /// </summary>
        public int TopLimit { get; set; } = 5;

        /// <summary>
        /// Filter from booking date (ISO format, optional)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter to booking date (ISO format, optional)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by partner ID (optional)
        /// </summary>
        public int? PartnerId { get; set; }

        /// <summary>
        /// Filter by cinema ID (optional)
        /// </summary>
        public int? CinemaId { get; set; }

        /// <summary>
        /// Search by customer email (optional)
        /// </summary>
        public string? CustomerEmail { get; set; }

        /// <summary>
        /// Search by customer name (optional)
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Page number for full list pagination (default: 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size for full list pagination (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort order for full list: "asc" or "desc" (default: "desc")
        /// </summary>
        public string SortOrder { get; set; } = "desc";

        /// <summary>
        /// Sort by for full list: "booking_count" or "total_spent" (default: "booking_count")
        /// </summary>
        public string SortBy { get; set; } = "booking_count";

        /// <summary>
        /// Sort order for top customers by booking count: "asc" or "desc" (default: "desc")
        /// </summary>
        public string TopByBookingCountSortOrder { get; set; } = "desc";

        /// <summary>
        /// Sort order for top customers by total spent: "asc" or "desc" (default: "desc")
        /// </summary>
        public string TopByTotalSpentSortOrder { get; set; } = "desc";
    }
}

