namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses
{
    /// <summary>
    /// Response DTO for detailed order information
    /// </summary>
    public class UserOrderDetailResponse
    {
        public OrderDetailBookingDto Booking { get; set; } = null!;
        public OrderDetailShowtimeDto Showtime { get; set; } = null!;
        public OrderDetailMovieDto Movie { get; set; } = null!;
        public OrderDetailCinemaDto Cinema { get; set; } = null!;
        public List<OrderDetailTicketDto> Tickets { get; set; } = new List<OrderDetailTicketDto>();
        public List<OrderDetailComboDto> Combos { get; set; } = new List<OrderDetailComboDto>();
        public OrderDetailVoucherDto? Voucher { get; set; }
    }

    /// <summary>
    /// Detailed booking information
    /// </summary>
    public class OrderDetailBookingDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public DateTime BookingTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TicketsTotal { get; set; } // Tổng tiền vé từ Ticket.Price
        public decimal CombosTotal { get; set; } // Tổng tiền combo từ ServiceOrder
        public string Status { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? PaymentStatus { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentTxId { get; set; }
        public int? VoucherId { get; set; }
        public string? OrderCode { get; set; }
        public Guid? SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Showtime information in order detail
    /// </summary>
    public class OrderDetailShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDatetime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public string? FormatType { get; set; }
    }

    /// <summary>
    /// Movie information in order detail
    /// </summary>
    public class OrderDetailMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public int DurationMinutes { get; set; }
        public string? Language { get; set; }
        public string? Director { get; set; }
        public string? Country { get; set; }
        public string? PosterUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Cinema information in order detail
    /// </summary>
    public class OrderDetailCinemaDto
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Ticket information with seat details
    /// </summary>
    public class OrderDetailTicketDto
    {
        public int TicketId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public string CheckInStatus { get; set; } = "NOT_CHECKED_IN"; // PENDING, CHECKED_IN, NO_SHOW
        public DateTime? CheckInTime { get; set; }
        public OrderDetailSeatDto Seat { get; set; } = null!;
    }

    /// <summary>
    /// Seat information in ticket
    /// </summary>
    public class OrderDetailSeatDto
    {
        public int SeatId { get; set; }
        public string RowCode { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string SeatName { get; set; } = null!;
        public string? SeatTypeName { get; set; }
    }

    /// <summary>
    /// Combo/Service information in order
    /// </summary>
    public class OrderDetailComboDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; } // Quantity * UnitPrice
    }

    /// <summary>
    /// Voucher information if applied
    /// </summary>
    public class OrderDetailVoucherDto
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountVal { get; set; }
    }
}

