namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests
{
    public class SubmitNewEmailRequest
    {
        public Guid RequestId { get; set; }
        public string NewEmail { get; set; } = string.Empty;
    }
}
