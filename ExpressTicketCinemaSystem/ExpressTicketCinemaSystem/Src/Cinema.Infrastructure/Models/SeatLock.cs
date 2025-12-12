using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class SeatLock
{
    public int ShowtimeId { get; set; }

    public int SeatId { get; set; }

    public Guid LockedBySession { get; set; }

    public DateTime LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BookingSession LockedBySessionNavigation { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;
}
