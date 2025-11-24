using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Cinema
{
    public int CinemaId { get; set; }

    public int PartnerId { get; set; }

    public string? CinemaName { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Code { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    public string? LogoUrl { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Partner Partner { get; set; } = null!;

    public virtual ICollection<RevenueReport> RevenueReports { get; set; } = new List<RevenueReport>();

    public virtual ICollection<Screen> Screens { get; set; } = new List<Screen>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual ICollection<EmployeeCinemaAssignment> EmployeeAssignments { get; set; } = new List<EmployeeCinemaAssignment>();
}
