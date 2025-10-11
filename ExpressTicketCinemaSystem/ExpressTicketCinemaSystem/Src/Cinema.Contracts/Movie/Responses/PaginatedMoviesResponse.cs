namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class PaginatedMoviesResponse
    {
        public string Message { get; set; }
        public MoviePaginatedResponse Result { get; set; }
    }
}
