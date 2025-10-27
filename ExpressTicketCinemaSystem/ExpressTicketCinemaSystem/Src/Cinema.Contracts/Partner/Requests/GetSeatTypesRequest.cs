namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class GetSeatTypesRequest
    {
        /// <summary>
        /// Page number (default: 1)
        /// </summary>
        /// <example>1</example>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default: 10, max: 100)
        /// </summary>
        /// <example>10</example>
        public int Limit { get; set; } = 10;

        /// <summary>
        /// Filter by seat type status
        /// </summary>
        /// <example>true</example>
        public bool? Status { get; set; }

        /// <summary>
        /// Filter by seat type code (e.g., STANDARD, VIP, COUPLE)
        /// </summary>
        /// <example>VIP</example>
        public string? Code { get; set; }

        /// <summary>
        /// Search term for seat type name or description
        /// </summary>
        /// <example>Ghế VIP</example>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by minimum surcharge amount
        /// </summary>
        /// <example>0</example>
        public decimal? MinSurcharge { get; set; }

        /// <summary>
        /// Filter by maximum surcharge amount
        /// </summary>
        /// <example>100000</example>
        public decimal? MaxSurcharge { get; set; }

        /// <summary>
        /// Field to sort by
        /// </summary>
        /// <example>name</example>
        public string? SortBy { get; set; } = "name";

        /// <summary>
        /// Sort order
        /// </summary>
        /// <example>asc</example>
        public string? SortOrder { get; set; } = "asc";
    }
}
