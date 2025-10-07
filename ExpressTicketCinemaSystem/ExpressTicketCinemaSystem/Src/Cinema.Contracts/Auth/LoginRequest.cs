namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth
{
    public class LoginRequest
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
