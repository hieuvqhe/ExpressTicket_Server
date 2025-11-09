using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? VoucherId { get; set; }

    public int ShowtimeId { get; set; }

    public int CustomerId { get; set; }

    public string BookingCode { get; set; } = null!;

    public DateTime BookingTime { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? OrderCode { get; set; }

    public Guid? SessionId { get; set; }

    public string? PricingSnapshot { get; set; }

    public string State { get; set; } = null!;

    public string? PaymentProvider { get; set; }

    public string? PaymentTxId { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Voucher? Voucher { get; set; }
}
