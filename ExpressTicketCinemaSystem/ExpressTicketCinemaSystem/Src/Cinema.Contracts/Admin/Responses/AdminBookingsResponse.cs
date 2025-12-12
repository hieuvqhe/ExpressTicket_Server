namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    /// <summary>
    /// Response DTO for admin bookings list with pagination
    /// </summary>
    public class AdminBookingsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<AdminBookingItemDto> Items { get; set; } = new List<AdminBookingItemDto>();
    }

    /// <summary>
    /// Individual booking item in admin's list
    /// </summary>
    public class AdminBookingItemDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public DateTime BookingTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? PaymentStatus { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentTxId { get; set; }
        public string? OrderCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TicketCount { get; set; }

        public AdminBookingCustomerDto Customer { get; set; } = null!;
        public AdminBookingShowtimeDto Showtime { get; set; } = null!;
        public AdminBookingCinemaDto Cinema { get; set; } = null!;
        public AdminBookingPartnerDto Partner { get; set; } = null!;
        public AdminBookingMovieDto Movie { get; set; } = null!;
    }

    /// <summary>
    /// Customer information in booking
    /// </summary>
    public class AdminBookingCustomerDto
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Showtime information in booking
    /// </summary>
    public class AdminBookingShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDatetime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? FormatType { get; set; }
        public string Status { get; set; } = null!;
    }

    /// <summary>
    /// Cinema information in booking
    /// </summary>
    public class AdminBookingCinemaDto
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
    }

    /// <summary>
    /// Partner information in booking
    /// </summary>
    public class AdminBookingPartnerDto
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? TaxCode { get; set; }
    }

    /// <summary>
    /// Movie information in booking
    /// </summary>
    public class AdminBookingMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public string? PosterUrl { get; set; }
        public string? Genre { get; set; }
    }
}

