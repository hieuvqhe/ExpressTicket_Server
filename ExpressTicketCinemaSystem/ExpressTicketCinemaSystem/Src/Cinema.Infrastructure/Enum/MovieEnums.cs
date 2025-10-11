using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum
{
    public class MovieEnums
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum MovieStatus
        {
            coming_soon,
            now_showing,
            ended
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum SortBy
        {
            title,
            premiere_date,
            average_rating,
            duration_minutes, 
            ratings_count,
            created_at
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum SortOrder
        {
            asc,
            desc
        }
    }
}
