namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    /// <summary>
    /// Request DTO for getting all bookings (Manager only) with filtering and pagination
    /// </summary>
    public class GetManagerBookingsRequest
    {
        /// <summary>
        /// Filter by partner ID
        /// </summary>
        public int? PartnerId { get; set; }

        /// <summary>
        /// Filter by cinema ID
        /// </summary>
        public int? CinemaId { get; set; }

        /// <summary>
        /// Filter by booking status (PAID, PENDING_PAYMENT, CANCELLED, FAILED, etc.)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by payment status (PAID, PENDING, FAILED)
        /// </summary>
        public string? PaymentStatus { get; set; }

        /// <summary>
        /// Filter from booking date (ISO format)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter to booking date (ISO format)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by customer ID
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Search by customer email
        /// </summary>
        public string? CustomerEmail { get; set; }

        /// <summary>
        /// Search by customer phone
        /// </summary>
        public string? CustomerPhone { get; set; }

        /// <summary>
        /// Search by customer name
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Search by booking code
        /// </summary>
        public string? BookingCode { get; set; }

        /// <summary>
        /// Search by order code (PayOS)
        /// </summary>
        public string? OrderCode { get; set; }

        /// <summary>
        /// Filter by movie ID
        /// </summary>
        public int? MovieId { get; set; }

        /// <summary>
        /// Filter by minimum amount
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Filter by maximum amount
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Page number (default 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default 20)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by: booking_time, total_amount, created_at, customer_name, partner_name, cinema_name (default booking_time)
        /// </summary>
        public string SortBy { get; set; } = "booking_time";

        /// <summary>
        /// Sort order: asc, desc (default desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }
}

