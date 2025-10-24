using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Manager
{
    public int ManagerId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;

    public string? Department { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual User User { get; set; } = null!;
}
