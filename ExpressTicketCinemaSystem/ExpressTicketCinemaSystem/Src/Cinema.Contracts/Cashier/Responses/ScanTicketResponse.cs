using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

public class ScanTicketResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public TicketScanInfo? TicketInfo { get; set; }
    public BookingCheckInStatus? BookingStatus { get; set; }
}

public class TicketScanInfo
{
    public int TicketId { get; set; }
    public int SeatId { get; set; }
    public string SeatName { get; set; } = null!;
    public string OrderCode { get; set; } = null!;
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = null!;
    public int ShowtimeId { get; set; }
    public DateTime ShowtimeStart { get; set; }
    public string MovieName { get; set; } = null!;
    public string CinemaName { get; set; } = null!;
    public DateTime? CheckInTime { get; set; }
    public bool IsAlreadyCheckedIn { get; set; }
}

public class BookingCheckInStatus
{
    public int BookingId { get; set; }
    public int TotalTickets { get; set; }
    public int CheckedInTickets { get; set; }
    public int NotCheckedInTickets { get; set; }
    public string BookingStatus { get; set; } = null!; // CONFIRMED, PARTIAL_CHECKED_IN, FULLY_CHECKED_IN
}























