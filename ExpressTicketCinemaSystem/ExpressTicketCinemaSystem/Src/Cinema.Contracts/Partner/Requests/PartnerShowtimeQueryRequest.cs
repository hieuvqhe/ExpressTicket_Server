namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class PartnerShowtimeQueryRequest
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? MovieId { get; set; }
        public string? CinemaId { get; set; }
        public string? ScreenId { get; set; }
        public string? Date { get; set; }
        public string? Status { get; set; }
        public string SortBy { get; set; } = "start_time";
        public string SortOrder { get; set; } = "asc";
    }
}