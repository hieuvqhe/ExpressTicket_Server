
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class SeatLayoutResponse
    {
        public SeatMapResponse SeatMap { get; set; } = new();
        public List<SeatResponse> Seats { get; set; } = new();
        public List<SeatTypeResponse> AvailableSeatTypes { get; set; } = new();
    }

    public class SeatMapResponse
    {
        public int SeatMapId { get; set; }
        public int ScreenId { get; set; }
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool HasLayout { get; set; }
    }

    public class SeatResponse
    {
        public int SeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Column { get; set; }
        public string? SeatName { get; set; }
        public int SeatTypeId { get; set; }
        public string SeatTypeCode { get; set; } = string.Empty;
        public string SeatTypeName { get; set; } = string.Empty;
        public string SeatTypeColor { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
    }

    public class ScreenSeatTypesResponse
    {
        public List<SeatTypeResponse> SeatTypes { get; set; } = new();
    }
}