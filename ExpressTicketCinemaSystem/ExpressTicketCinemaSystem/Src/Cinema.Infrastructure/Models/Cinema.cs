using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Cinema
{
    public int CinemaId { get; set; }

    public int PartnerId { get; set; }

    public string CinemaName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Partner Partner { get; set; } = null!;

    public virtual ICollection<RevenueReport> RevenueReports { get; set; } = new List<RevenueReport>();

    public virtual ICollection<Screen> Screens { get; set; } = new List<Screen>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
