using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PaginatedCinemasResponse
    {
        public List<CinemaResponse> Cinemas { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}