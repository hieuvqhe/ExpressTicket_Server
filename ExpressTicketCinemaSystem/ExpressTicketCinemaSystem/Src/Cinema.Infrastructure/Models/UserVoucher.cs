using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class UserVoucher
{
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int UserId { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }

    public int? BookingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Voucher Voucher { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Booking? Booking { get; set; }
}


















