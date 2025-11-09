using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public decimal DiscountVal { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    // Các trường mới được thêm
    public int ManagerId { get; set; }

    public string DiscountType { get; set; } = "fixed";

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Manager Manager { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<VoucherEmailHistory> VoucherEmailHistories { get; set; } = new List<VoucherEmailHistory>();
}
