using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class GameShow
{
    public int GameshowId { get; set; }

    public int PartnerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual Partner Partner { get; set; } = null!;
}
