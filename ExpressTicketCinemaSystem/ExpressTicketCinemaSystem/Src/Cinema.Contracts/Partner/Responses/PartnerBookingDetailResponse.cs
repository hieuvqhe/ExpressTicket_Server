namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    /// <summary>
    /// Response DTO for partner booking detail
    /// </summary>
    public class PartnerBookingDetailResponse
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

        public PartnerBookingCustomerDto Customer { get; set; } = null!;
        public PartnerBookingShowtimeDto Showtime { get; set; } = null!;
        public PartnerBookingCinemaDto Cinema { get; set; } = null!;
        public PartnerBookingMovieDto Movie { get; set; } = null!;
        public PartnerBookingVoucherDto? Voucher { get; set; }

        public List<PartnerBookingTicketDto> Tickets { get; set; } = new List<PartnerBookingTicketDto>();
        public List<PartnerBookingServiceOrderDto> ServiceOrders { get; set; } = new List<PartnerBookingServiceOrderDto>();
    }

    /// <summary>
    /// Ticket information in booking detail
    /// </summary>
    public class PartnerBookingTicketDto
    {
        public int TicketId { get; set; }
        public int SeatId { get; set; }
        public string SeatName { get; set; } = null!;
        public string RowCode { get; set; } = null!;
        public int SeatNumber { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
    }

    /// <summary>
    /// Service order information in booking detail
    /// </summary>
    public class PartnerBookingServiceOrderDto
    {
        public int OrderId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? ServiceCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    /// <summary>
    /// Voucher information in booking detail
    /// </summary>
    public class PartnerBookingVoucherDto
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
    }
}

