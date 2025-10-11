namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class TopRatedMovieResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public string? PosterUrl { get; set; }
        public DateTime? PremiereDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }
}