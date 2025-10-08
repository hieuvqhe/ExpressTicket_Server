namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie
{
    public class TopRatedMovieResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public string? PosterUrl { get; set; }
        public DateOnly? ReleaseDate { get; set; }

        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }
}
