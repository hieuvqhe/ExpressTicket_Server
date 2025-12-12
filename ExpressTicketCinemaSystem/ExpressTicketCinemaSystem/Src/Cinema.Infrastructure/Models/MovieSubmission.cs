using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
public partial class MovieSubmission
{
    public int MovieSubmissionId { get; set; }

    public int PartnerId { get; set; }

    public string Title { get; set; } = null!;

    public string Genre { get; set; } = null!;

    public int DurationMinutes { get; set; }

    public string Director { get; set; } = null!;

    public string Language { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string? PosterUrl { get; set; }

    public string? BannerUrl { get; set; }

    public string Production { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateOnly PremiereDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? TrailerUrl { get; set; }

    public string? CopyrightDocumentUrl { get; set; }

    public string? DistributionLicenseUrl { get; set; }

    public string? AdditionalNotes { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewerId { get; set; }

    public int? ManagerStaffId { get; set; }

    public string? RejectionReason { get; set; }

    public int? MovieId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResubmittedAt { get; set; }

    public int ResubmitCount { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual ICollection<MovieSubmissionActor> MovieSubmissionActors { get; set; } = new List<MovieSubmissionActor>();

    public virtual Partner Partner { get; set; } = null!;

    public virtual Manager? Reviewer { get; set; }

    public virtual ManagerStaff? ManagerStaff { get; set; }
}

