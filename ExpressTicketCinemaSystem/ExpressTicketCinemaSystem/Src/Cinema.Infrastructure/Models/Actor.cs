using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Actor
{
    public int ActorId { get; set; }

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public virtual ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
}
