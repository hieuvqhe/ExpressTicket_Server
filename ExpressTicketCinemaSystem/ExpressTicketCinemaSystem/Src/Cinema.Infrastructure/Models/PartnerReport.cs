using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class PartnerReport
{
    public int ReportId { get; set; }

    public int PartnerId { get; set; }

    public DateOnly ReportDate { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal NetRevenue { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Partner Partner { get; set; } = null!;
}
