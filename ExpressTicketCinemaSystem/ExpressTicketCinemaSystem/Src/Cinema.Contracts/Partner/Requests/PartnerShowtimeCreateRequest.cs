
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class PartnerShowtimeCreateRequest
    {
        public int MovieId { get; set; }
        public int ScreenId { get; set; }
        public int CinemaId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public int AvailableSeats { get; set; }
        public string FormatType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
