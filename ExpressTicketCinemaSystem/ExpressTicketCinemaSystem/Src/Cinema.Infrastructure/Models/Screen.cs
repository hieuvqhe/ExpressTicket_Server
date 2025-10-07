using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Screen
{
    public int ScreenId { get; set; }

    public int CinemaId { get; set; }

    public string ScreenName { get; set; } = null!;

    public string? ScreenType { get; set; }

    public bool IsActive { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual SeatMap? SeatMap { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
