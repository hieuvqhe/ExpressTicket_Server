using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class GetScreenResponse
    {
        [JsonPropertyName("screen_id")]
        public int ScreenId { get; set; }

        [JsonPropertyName("cinema_id")]
        public int CinemaId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("seat_layout")]
        public List<List<SeatLayoutResponse>> SeatLayout { get; set; } = new();

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

}