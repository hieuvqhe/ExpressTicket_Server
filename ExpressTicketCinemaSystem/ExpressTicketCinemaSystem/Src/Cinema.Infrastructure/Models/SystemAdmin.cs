using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class SystemAdmin
{
    public int AdminId { get; set; }

    public int UserId { get; set; }

    public string? AdminLevel { get; set; }

    public string? Permissions { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
