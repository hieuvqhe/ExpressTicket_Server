// Thêm file: ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses/PartnerShowtimeDetailResponse.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PartnerShowtimeDetailResponse
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public int ScreenId { get; set; }
        public int CinemaId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public string FormatType { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public string Status { get; set; } = string.Empty;
        public ShowtimeMovieInfo Movie { get; set; } = new();
        public ShowtimeCinemaInfo Cinema { get; set; } = new();
        public ShowtimeScreenInfo Screen { get; set; } = new();
    }

    public class ShowtimeMovieInfo
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public class ShowtimeCinemaInfo
    {
        public int CinemaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ShowtimeScreenInfo
    {
        public int ScreenId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public string SoundSystem { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SeatRows { get; set; }
        public int SeatColumns { get; set; }
        public int Capacity { get; set; }
    }
}