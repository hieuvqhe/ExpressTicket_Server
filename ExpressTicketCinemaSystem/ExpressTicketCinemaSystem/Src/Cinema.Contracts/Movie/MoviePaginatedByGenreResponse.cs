namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie
{
    public class MoviePaginatedByGenreResponse
    {
        public List<MovieResponse> Movies { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
        public string Genre { get; set; }
    }

}
