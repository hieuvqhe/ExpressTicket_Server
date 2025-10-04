using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

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

    public virtual Customer Customer { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Voucher? Voucher { get; set; }
}
