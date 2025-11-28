namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses
{
    /// <summary>
    /// Response DTO for user tickets list with pagination
    /// </summary>
    public class UserTicketsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<UserTicketItemDto> Items { get; set; } = new List<UserTicketItemDto>();
    }

    /// <summary>
    /// Individual ticket item in the list
    /// </summary>
    public class UserTicketItemDto
    {
        public int TicketId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public string CheckInStatus { get; set; } = "NOT_CHECKED_IN"; // PENDING, CHECKED_IN, NO_SHOW
        public DateTime? CheckInTime { get; set; }
        
        public TicketBookingDto Booking { get; set; } = null!;
        public TicketMovieDto Movie { get; set; } = null!;
        public TicketCinemaDto Cinema { get; set; } = null!;
        public TicketShowtimeDto Showtime { get; set; } = null!;
        public TicketSeatDto Seat { get; set; } = null!;
    }

    /// <summary>
    /// Booking information in ticket
    /// </summary>
    public class TicketBookingDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public string? PaymentStatus { get; set; }
    }

    /// <summary>
    /// Movie information in ticket
    /// </summary>
    public class TicketMovieDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public string? PosterUrl { get; set; }
        public string? Genre { get; set; }
    }

    /// <summary>
    /// Cinema information in ticket
    /// </summary>
    public class TicketCinemaDto
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
    }

    /// <summary>
    /// Showtime information in ticket
    /// </summary>
    public class TicketShowtimeDto
    {
        public int ShowtimeId { get; set; }
        public DateTime ShowDatetime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? FormatType { get; set; }
        public string Status { get; set; } = null!;
    }

    /// <summary>
    /// Seat information in ticket
    /// </summary>
    public class TicketSeatDto
    {
        public int SeatId { get; set; }
        public string RowCode { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string SeatName { get; set; } = null!;
        public string? SeatTypeName { get; set; }
    }
}

