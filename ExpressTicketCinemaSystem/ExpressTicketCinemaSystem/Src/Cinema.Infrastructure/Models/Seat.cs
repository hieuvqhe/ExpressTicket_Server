using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int ScreenId { get; set; }

    public string RowCode { get; set; } = null!;

    public int SeatNumber { get; set; }

    public string Status { get; set; } = null!;

    public int? SeatTypeId { get; set; }

    public virtual Screen Screen { get; set; } = null!;

    public virtual SeatType? SeatType { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

