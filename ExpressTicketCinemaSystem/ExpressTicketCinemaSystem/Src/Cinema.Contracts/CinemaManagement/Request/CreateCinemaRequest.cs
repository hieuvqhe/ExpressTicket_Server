namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Requests
{
    public class CreateCinemaRequest
    {
        public string CinemaName { get; set; }
        public string Address { get; set; }
        public string? Phone { get; set; }
        public int PartnerId { get; set; }
    }
}
