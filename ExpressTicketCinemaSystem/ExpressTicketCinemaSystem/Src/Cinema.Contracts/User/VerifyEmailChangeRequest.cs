namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User
{
    public class VerifyEmailChangeRequest
    {
        public Guid RequestId { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
