namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class MovieListResponse
    {
        public string Message { get; set; } = string.Empty;
        public MoviePaginatedResponse Result { get; set; } = new();
    }

}
