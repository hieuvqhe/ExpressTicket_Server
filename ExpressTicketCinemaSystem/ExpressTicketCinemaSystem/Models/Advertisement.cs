using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

public partial class Advertisement
{
    public int AdId { get; set; }

    public int PartnerId { get; set; }

    public string AdTitle { get; set; } = null!;

    public string AdType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal? Cost { get; set; }

    public string? Status { get; set; }

    public virtual Partner Partner { get; set; } = null!;
}
