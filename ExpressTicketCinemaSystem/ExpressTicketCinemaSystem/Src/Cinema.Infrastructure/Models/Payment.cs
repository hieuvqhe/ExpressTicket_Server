﻿using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime PaidAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
