using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VoucherReservation
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public Guid SessionId { get; set; }

    public int UserId { get; set; }

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    // Navigation properties
    public virtual Voucher Voucher { get; set; } = null!;

    public virtual BookingSession Session { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}


























