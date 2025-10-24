using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UpdateScreenRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("seat_layout")]
        public List<List<SeatLayoutRequest>> SeatLayout { get; set; } = new();

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [JsonPropertyName("screen_type")]
        public string ScreenType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
