namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

/// <summary>
/// Response DTO for cashier bookings list with pagination
/// </summary>
public class CashierBookingsResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<CashierBookingItemDto> Items { get; set; } = new List<CashierBookingItemDto>();
}

/// <summary>
/// Individual booking item in cashier's list
/// </summary>
public class CashierBookingItemDto
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = null!;
    public string? OrderCode { get; set; }
    public DateTime BookingTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!; // CONFIRMED, PARTIAL_CHECKED_IN, FULLY_CHECKED_IN
    public string? PaymentStatus { get; set; }
    public string? PaymentProvider { get; set; }
    public int TicketCount { get; set; }
    public int CheckedInTicketCount { get; set; }
    public int NotCheckedInTicketCount { get; set; }
    
    public CashierBookingShowtimeDto Showtime { get; set; } = null!;
    public CashierBookingMovieDto Movie { get; set; } = null!;
    public CashierBookingCustomerDto Customer { get; set; } = null!;
}

/// <summary>
/// Showtime information in booking
/// </summary>
public class CashierBookingShowtimeDto
{
    public int ShowtimeId { get; set; }
    public DateTime ShowDatetime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FormatType { get; set; }
    public string Status { get; set; } = null!;
}

/// <summary>
/// Movie information in booking
/// </summary>
public class CashierBookingMovieDto
{
    public int MovieId { get; set; }
    public string Title { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
}

/// <summary>
/// Customer information in booking
/// </summary>
public class CashierBookingCustomerDto
{
    public int CustomerId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}



















