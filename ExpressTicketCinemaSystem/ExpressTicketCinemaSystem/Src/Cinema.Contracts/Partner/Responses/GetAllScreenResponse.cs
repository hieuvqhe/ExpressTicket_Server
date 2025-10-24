using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class GetAllScreenResponse
    {
        [JsonPropertyName("screens")]
        public List<ScreenItemResponse> Screens { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    public class ScreenItemResponse
    {
        [JsonPropertyName("screen_id")]
        public int ScreenId { get; set; }

        [JsonPropertyName("cinema_id")]
        public int CinemaId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("seat_layout")]
        public List<List<GetAllSeatLayoutResponse>> SeatLayout { get; set; } = new();

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("screen_type")]
        public string ScreenType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class GetAllSeatLayoutResponse
    {
        [JsonPropertyName("row")]
        public string Row { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}