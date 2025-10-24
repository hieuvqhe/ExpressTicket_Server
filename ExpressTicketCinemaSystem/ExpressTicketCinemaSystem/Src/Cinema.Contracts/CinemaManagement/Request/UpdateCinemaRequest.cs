namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.TheaterManagement.Requests
{
    public class UpdateCinemaRequest
    {
        public string? CinemaName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? TotalRooms { get; set; }
    }
}
