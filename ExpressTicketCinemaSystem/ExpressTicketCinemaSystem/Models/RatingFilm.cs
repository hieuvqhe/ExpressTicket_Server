using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

public partial class RatingFilm
{
    public int RatingId { get; set; }

    public int MovieId { get; set; }

    public int UserId { get; set; }

    public string? Comment { get; set; }

    public int RatingStar { get; set; }

    public DateTime RatingAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
