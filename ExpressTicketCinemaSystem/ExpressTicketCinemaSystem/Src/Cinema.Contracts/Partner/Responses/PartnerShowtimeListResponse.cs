namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PartnerShowtimeListResponse
    {
        public List<PartnerShowtimeListItem> Showtimes { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
    }

    public class PartnerShowtimeListItem
    {
        public string ShowtimeId { get; set; } = string.Empty;
        public string MovieId { get; set; } = string.Empty;
        public string ScreenId { get; set; } = string.Empty;
        public string CinemaId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BasePrice { get; set; } = string.Empty;
        public string FormatType { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public string Status { get; set; } = string.Empty;
        public ShowtimeMovieInfo Movie { get; set; } = new();
        public ShowtimeCinemaInfo Cinema { get; set; } = new();
        public ShowtimeScreenInfo Screen { get; set; } = new();
    }
}