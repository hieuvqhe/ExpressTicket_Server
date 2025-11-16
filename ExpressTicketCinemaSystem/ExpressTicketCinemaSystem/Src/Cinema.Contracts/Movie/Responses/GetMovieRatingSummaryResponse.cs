using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class GetMovieRatingSummaryResponse
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("average_rating")]
        public decimal? AverageRating { get; set; }

        [JsonPropertyName("total_ratings")]
        public int TotalRatings { get; set; }

        [JsonPropertyName("breakdown")]
        public Dictionary<string, int> Breakdown { get; set; } = new();
    }
}

