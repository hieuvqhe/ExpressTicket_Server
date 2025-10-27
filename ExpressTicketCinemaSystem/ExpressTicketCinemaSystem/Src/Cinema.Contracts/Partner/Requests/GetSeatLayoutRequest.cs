namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class GetSeatLayoutRequest
    {
        // Có thể thêm các filter parameter sau này
        public bool? IncludeInactiveSeats { get; set; } = false;
    }
}