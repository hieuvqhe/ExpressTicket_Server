using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Screen
{
    public int ScreenId { get; set; }

    public int CinemaId { get; set; }

    public string ScreenName { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Code { get; set; }

    public string? Description { get; set; }

    public int? Capacity { get; set; }

    public int? SeatRows { get; set; }

    public int? SeatColumns { get; set; }

    public string? ScreenType { get; set; }

    public string? SoundSystem { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual SeatMap? SeatMap { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
