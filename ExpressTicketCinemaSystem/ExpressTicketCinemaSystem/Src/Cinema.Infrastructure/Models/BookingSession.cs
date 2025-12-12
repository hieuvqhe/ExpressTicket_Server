using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class BookingSession
{
    public Guid Id { get; set; }

    public int? UserId { get; set; }

    public int ShowtimeId { get; set; }

    public string ItemsJson { get; set; } = null!;

    public string? CouponCode { get; set; }

    public string PricingJson { get; set; } = null!;

    public string State { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<SeatLock> SeatLocks { get; set; } = new List<SeatLock>();

    public virtual Showtime Showtime { get; set; } = null!;
}
