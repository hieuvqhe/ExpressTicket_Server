using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class SeatTicket
{
    public int SeatTicketId { get; set; }

    public int TicketId { get; set; }

    public int BookingId { get; set; }

    public int SeatId { get; set; }

    public int ShowtimeId { get; set; }

    public string OrderCode { get; set; } = null!;

    public string? CheckInStatus { get; set; } // CHECKED_IN, NOT_CHECKED_IN, PARTIAL_CHECKED

    public DateTime? CheckInTime { get; set; }

    public int? CheckedInBy { get; set; } // EmployeeId của cashier

    public int CinemaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual Employee? CheckedInByEmployee { get; set; }
}























