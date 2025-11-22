namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    /// <summary>
    /// Request DTO for getting booking statistics (Manager only)
    /// </summary>
    public class GetManagerBookingStatisticsRequest
    {
        /// <summary>
        /// Filter from booking date (ISO format). Default: 30 days ago
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter to booking date (ISO format). Default: today
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by partner ID (null = all partners)
        /// </summary>
        public int? PartnerId { get; set; }

        /// <summary>
        /// Filter by cinema ID (null = all cinemas)
        /// </summary>
        public int? CinemaId { get; set; }

        /// <summary>
        /// Filter by movie ID (null = all movies)
        /// </summary>
        public int? MovieId { get; set; }

        /// <summary>
        /// Number of top items to return (default: 10, max: 50)
        /// </summary>
        public int TopLimit { get; set; } = 10;

        /// <summary>
        /// Group by time period for trends: day, week, month (default: day)
        /// </summary>
        public string GroupBy { get; set; } = "day";

        /// <summary>
        /// Include comparison with previous period (default: true)
        /// </summary>
        public bool IncludeComparison { get; set; } = true;

        /// <summary>
        /// Page number for paginated lists (CinemaRevenueList, PartnerRevenueList) (default: 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size for paginated lists (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

