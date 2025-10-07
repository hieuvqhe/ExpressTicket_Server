using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Cast
{
    public int CastId { get; set; }

    public string Name { get; set; } = null!;

    public string? Character { get; set; }

    public string? Gender { get; set; }
}
