using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Showtime
{
    public int ShowtimeId { get; set; }

    public int CinemaId { get; set; }

    public int ScreenId { get; set; }

    public int MovieId { get; set; }

    public DateTime ShowDatetime { get; set; }

    public decimal BasePrice { get; set; }

    public string Status { get; set; } = null!;

    // Các trường mới được thêm
    public DateTime? EndTime { get; set; }

    public int? AvailableSeats { get; set; }

    public string? FormatType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;

    public virtual Screen Screen { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}