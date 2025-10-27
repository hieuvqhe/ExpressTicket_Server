namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Responses
{
    public class CinemaResponse
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
