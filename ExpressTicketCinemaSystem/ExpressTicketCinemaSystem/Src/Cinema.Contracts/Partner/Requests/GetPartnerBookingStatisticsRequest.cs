namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    /// <summary>
    /// Request DTO for getting partner's booking statistics
    /// </summary>
    public class GetPartnerBookingStatisticsRequest
    {
        /// <summary>
        /// Filter from booking date (ISO format). If null, defaults to 30 days ago
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter to booking date (ISO format). If null, defaults to today
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by cinema ID (if null, get statistics for all partner's cinemas)
        /// </summary>
        public int? CinemaId { get; set; }

        /// <summary>
        /// Group by time period: "day", "week", "month", "year" (default: "day")
        /// </summary>
        public string GroupBy { get; set; } = "day";

        /// <summary>
        /// Include comparison with previous period (default: false)
        /// </summary>
        public bool IncludeComparison { get; set; } = false;

        /// <summary>
        /// Limit for top items (default: 10)
        /// </summary>
        public int TopLimit { get; set; } = 10;

        /// <summary>
        /// Page number for paginated lists (default: 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size for paginated lists (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

