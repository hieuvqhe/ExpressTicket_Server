namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie
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
        public List<ActorDto> Actor { get; set; }
    }
}