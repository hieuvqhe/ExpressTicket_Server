using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

public class BookingDetailsResponse
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = null!;
    public string? OrderCode { get; set; }
    public string Status { get; set; } = null!;
    public DateTime BookingTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentStatus { get; set; }
    public ShowtimeInfo Showtime { get; set; } = null!;
    public CustomerInfo Customer { get; set; } = null!;
    public List<TicketDetail> Tickets { get; set; } = new List<TicketDetail>();
    public BookingCheckInSummary CheckInSummary { get; set; } = null!;
}

public class ShowtimeInfo
{
    public int ShowtimeId { get; set; }
    public DateTime ShowDatetime { get; set; }
    public string MovieName { get; set; } = null!;
    public string CinemaName { get; set; } = null!;
    public string ScreenName { get; set; } = null!;
}

public class CustomerInfo
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class TicketDetail
{
    public int TicketId { get; set; }
    public int SeatId { get; set; }
    public string SeatName { get; set; } = null!;
    public decimal Price { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? CheckInTime { get; set; }
    public string? CheckedInByEmployeeName { get; set; }
    public string CheckInStatus { get; set; } = null!; // CHECKED_IN, NOT_CHECKED_IN
}

public class BookingCheckInSummary
{
    public int TotalTickets { get; set; }
    public int CheckedInTickets { get; set; }
    public int NotCheckedInTickets { get; set; }
    public string BookingCheckInStatus { get; set; } = null!; // CONFIRMED, PARTIAL_CHECKED_IN, FULLY_CHECKED_IN
}























