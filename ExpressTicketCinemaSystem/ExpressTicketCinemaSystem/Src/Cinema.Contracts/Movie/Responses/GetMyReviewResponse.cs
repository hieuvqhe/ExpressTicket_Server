using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class GetMyReviewResponse
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("review")]
        public MyReviewDetail? Review { get; set; }
    }

    public class MyReviewDetail
    {
        [JsonPropertyName("rating_id")]
        public int RatingId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("user_avatar")]
        public string? UserAvatar { get; set; }

        [JsonPropertyName("rating_star")]
        public int RatingStar { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("rating_at")]
        public DateTime RatingAt { get; set; }

        [JsonPropertyName("image_urls")]
        public List<string>? ImageUrls { get; set; }
    }
}


