using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class CreateReviewResponse
    {
        [JsonPropertyName("rating_id")]
        public int RatingId { get; set; }

        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("rating_star")]
        public int RatingStar { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("rating_at")]
        public DateTime RatingAt { get; set; }
    }
}

