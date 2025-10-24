using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class CreateScreenResponse
    {
        [JsonPropertyName("screen_id")]
        public int ScreenId { get; set; }
    }
}