using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class SeatMap
{
    public int SeatMapId { get; set; }

    public int ScreenId { get; set; }

    public string? LayoutData { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int TotalRows { get; set; }

    public int TotalColumns { get; set; }

    public virtual Screen Screen { get; set; } = null!;
}

