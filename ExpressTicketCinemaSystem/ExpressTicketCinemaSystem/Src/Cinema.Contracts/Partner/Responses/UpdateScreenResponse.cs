using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class UpdateScreenResponse
    {
        [JsonPropertyName("screen_id")]
        public int ScreenId { get; set; }
    }
}
