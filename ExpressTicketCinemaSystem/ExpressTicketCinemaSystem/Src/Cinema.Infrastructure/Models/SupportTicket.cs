using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class SupportTicket
{
    public int TicketId { get; set; }

    public int? BookingId { get; set; }

    public int? CustomerId { get; set; }

    public string Subject { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
