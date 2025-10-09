namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User
{
    public class RequestEmailChangeResponse
    {
        public Guid RequestId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool CurrentVerified { get; set; } = false;
    }
}
