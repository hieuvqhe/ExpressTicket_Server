using System;
using System.Collections.Generic;
namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
public partial class MovieSubmissionActor
{
    public int MovieSubmissionActorId { get; set; }

    public int MovieSubmissionId { get; set; }

    public int? ActorId { get; set; }

    public string ActorName { get; set; } = null!;

    public string? ActorAvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public virtual Actor? Actor { get; set; }

    public virtual MovieSubmission MovieSubmission { get; set; } = null!;
}

