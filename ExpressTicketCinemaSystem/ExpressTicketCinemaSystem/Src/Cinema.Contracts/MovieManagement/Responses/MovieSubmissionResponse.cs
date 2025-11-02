using System.Text.Json.Serialization;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses
{
    public class MovieSubmissionResponse
    {
        public int MovieSubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Director { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public string? PosterUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Production { get; set; }
        public string? Description { get; set; }

        // DB đang là DATE NOT NULL -> để DateOnly
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateOnly PremiereDate { get; set; }

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateOnly EndDate { get; set; }

        public string? TrailerUrl { get; set; }
        public string? CopyrightDocumentUrl { get; set; }
        public string? DistributionLicenseUrl { get; set; }
        public string? AdditionalNotes { get; set; }

        public string Status { get; set; } = string.Empty;

        // Mốc thời gian dùng DateTime (datetime2)
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string? RejectionReason { get; set; }
        public int? MovieId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<SubmissionActorResponse> Actors { get; set; } = new();
    }

    public class PaginatedMovieSubmissionsResponse
    {
        public List<MovieSubmissionResponse> Submissions { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }

    public class SubmitMovieSubmissionResponse
    {
        public int MovieSubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
