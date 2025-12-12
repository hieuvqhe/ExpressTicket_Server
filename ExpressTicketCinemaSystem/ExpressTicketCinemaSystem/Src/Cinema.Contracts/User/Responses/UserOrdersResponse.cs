namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses
{
    /// <summary>
    /// Response DTO for user orders list with pagination
    /// </summary>
    public class UserOrdersResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<UserOrderItemDto> Items { get; set; } = new List<UserOrderItemDto>();
    }

    /// <summary>
    /// Individual order item in the list
    /// </summary>
    public class UserOrderItemDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public DateTime BookingTime { get; set; }
        public string Status { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public int TicketCount { get; set; }

        public OrderShowtimeDto Showtime { get; set; } = null!;
        public OrderMovieDto Movie { get; set; } = null!;
        public OrderCinemaDto Cinema { get; set; } = null!;
    }

    /// <summary>
    /// Showtime information in order
    /// </summary>
    public class OrderShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDatetime { get; set; }
        public string? FormatType { get; set; }
    }

    /// <summary>
    /// Movie information in order
    /// </summary>
    public class OrderMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public string? PosterUrl { get; set; }
    }

    /// <summary>
    /// Cinema information in order
    /// </summary>
    public class OrderCinemaDto
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
    }
}

