namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Full detailed response for a single booking (for Manager role)
    /// </summary>
    public class BookingDetailResponse
    {
        public BookingInfoDto Booking { get; set; } = null!;
        public BookingDetailCustomerDto Customer { get; set; } = null!;
        public BookingDetailShowtimeDto Showtime { get; set; } = null!;
        public BookingDetailCinemaDto Cinema { get; set; } = null!;
        public BookingDetailScreenDto Screen { get; set; } = null!;
        public BookingDetailPartnerDto Partner { get; set; } = null!;
        public BookingDetailMovieDto Movie { get; set; } = null!;
        public List<BookingTicketDto> Tickets { get; set; } = new List<BookingTicketDto>();
        public List<BookingServiceOrderDto> ServiceOrders { get; set; } = new List<BookingServiceOrderDto>();
        public BookingPaymentDto? Payment { get; set; }
        public BookingVoucherDto? Voucher { get; set; }
        public PricingBreakdownDto PricingBreakdown { get; set; } = null!;
    }

    /// <summary>
    /// Core booking information
    /// </summary>
    public class BookingInfoDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public DateTime BookingTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? PaymentProvider { get; set; }
        public string? PaymentTxId { get; set; }
        public string? PaymentStatus { get; set; }
        public string? OrderCode { get; set; }
        public Guid? SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Customer information with full details
    /// </summary>
    public class BookingDetailCustomerDto
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Username { get; set; }
    }

    /// <summary>
    /// Showtime information with full details
    /// </summary>
    public class BookingDetailShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDatetime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = null!;
        public string? FormatType { get; set; }
        public int? AvailableSeats { get; set; }
    }

    /// <summary>
    /// Cinema information with full details
    /// </summary>
    public class BookingDetailCinemaDto
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Screen information with full details
    /// </summary>
    public class BookingDetailScreenDto
    {
        public int ScreenId { get; set; }
        public string ScreenName { get; set; } = null!;
        public string? Code { get; set; }
        public int? Capacity { get; set; }
        public string? ScreenType { get; set; }
        public string? SoundSystem { get; set; }
    }

    /// <summary>
    /// Partner information with full details
    /// </summary>
    public class BookingDetailPartnerDto
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? TaxCode { get; set; }
        public string Status { get; set; } = null!;
        public decimal? CommissionRate { get; set; }
    }

    /// <summary>
    /// Movie information with full details
    /// </summary>
    public class BookingDetailMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public int DurationMinutes { get; set; }
        public string? Director { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public string? PosterUrl { get; set; }
        public DateOnly? PremiereDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    /// <summary>
    /// Ticket information with seat details
    /// </summary>
    public class BookingTicketDto
    {
        public int TicketId { get; set; }
        public int SeatId { get; set; }
        public string? SeatName { get; set; }
        public string RowCode { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string? SeatTypeName { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
    }

    /// <summary>
    /// Service order (combo) information
    /// </summary>
    public class BookingServiceOrderDto
    {
        public int OrderId { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    /// <summary>
    /// Payment information
    /// </summary>
    public class BookingPaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Provider { get; set; }
        public string? TransactionId { get; set; }
        public DateTime PaidAt { get; set; }
        public bool? SignatureOk { get; set; }
    }

    /// <summary>
    /// Voucher information
    /// </summary>
    public class BookingVoucherDto
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountVal { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }

    /// <summary>
    /// Pricing breakdown showing calculation details
    /// </summary>
    public class PricingBreakdownDto
    {
        public decimal TicketsSubtotal { get; set; }
        public decimal ServicesSubtotal { get; set; }
        public decimal SubtotalBeforeVoucher { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal FinalTotal { get; set; }
        public decimal? CommissionAmount { get; set; }
        public decimal? CommissionRate { get; set; }
    }
}

