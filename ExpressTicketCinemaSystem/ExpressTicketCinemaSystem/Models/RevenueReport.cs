using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

public partial class RevenueReport
{
    public int ReportId { get; set; }

    public int CinemaId { get; set; }

    public DateOnly ReportDate { get; set; }

    public decimal TicketRevenue { get; set; }

    public decimal ServiceRevenue { get; set; }

    public int TotalTickets { get; set; }

    public decimal OccupancyRate { get; set; }

    public DateTime GeneratedAt { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;
}
