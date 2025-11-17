namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    /// <summary>
    /// Request DTO for getting partner's bookings with filtering and pagination
    /// </summary>
    public class GetPartnerBookingsRequest
    {
        /// <summary>
        /// Filter by cinema ID (if null, get all cinemas of partner)
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
        /// Search by booking code
        /// </summary>
        public string? BookingCode { get; set; }

        /// <summary>
        /// Page number (default 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page (default 20)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by: booking_time, total_amount, created_at (default booking_time)
        /// </summary>
        public string SortBy { get; set; } = "booking_time";

        /// <summary>
        /// Sort order: asc, desc (default desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }
}

