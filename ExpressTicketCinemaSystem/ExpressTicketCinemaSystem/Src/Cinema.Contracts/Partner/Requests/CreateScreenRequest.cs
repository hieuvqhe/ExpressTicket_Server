using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateScreenRequest
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

    public class SeatLayoutRequest
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