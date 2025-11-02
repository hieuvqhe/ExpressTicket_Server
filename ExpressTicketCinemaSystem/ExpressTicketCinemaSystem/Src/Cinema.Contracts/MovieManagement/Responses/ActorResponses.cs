using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;

public class SubmissionActorResponse
{
    public int MovieSubmissionActorId { get; set; }
    public int? ActorId { get; set; } 
    public string ActorName { get; set; } = string.Empty;
    public string? ActorAvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsExistingActor { get; set; }
}

public class SubmissionActorsListResponse
{
    public List<SubmissionActorResponse> Actors { get; set; } = new();
    public int TotalCount { get; set; }
}

// Response cho Available Actors
public class AvailableActorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
}

public class PaginatedAvailableActorsResponse
{
    public List<AvailableActorResponse> Actors { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}