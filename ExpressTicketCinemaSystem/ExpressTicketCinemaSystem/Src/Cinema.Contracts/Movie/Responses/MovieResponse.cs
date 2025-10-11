namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class MovieResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public int DurationMinutes { get; set; }
        public DateOnly PremiereDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? Director { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; }
        public string? PosterUrl { get; set; }
        public string? Production { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; }

        public string? TrailerUrl { get; set; }
        public List<ActorResponse> Actor { get; set; }
        public double? AverageRating { get; set; }
        public int? RatingsCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}