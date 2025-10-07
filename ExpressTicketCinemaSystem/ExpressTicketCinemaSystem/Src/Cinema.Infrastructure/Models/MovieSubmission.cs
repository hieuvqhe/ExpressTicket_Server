using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class MovieSubmission
{
    public int SubmissionId { get; set; }

    public int PartnerId { get; set; }

    public int? MovieId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual Partner Partner { get; set; } = null!;
}
