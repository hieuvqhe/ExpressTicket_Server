namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests
{
    public class VerifyResetCodeRequest
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
