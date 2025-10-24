namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Responses
{
    public class PaginatedCinemasResponse
    {
        public IEnumerable<CinemaResponse> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
    }
}
