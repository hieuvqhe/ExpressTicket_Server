using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
public partial class SeatType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Surcharge { get; set; }

    public string Color { get; set; } = null!;

    public string? Description { get; set; }

    public bool Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int PartnerId { get; set; }

    public virtual Partner Partner { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

