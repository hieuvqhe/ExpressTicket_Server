using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class ActorListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
    }

    public class PaginatedActorsResponse
    {
        public List<ActorListResponse> Actors { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}