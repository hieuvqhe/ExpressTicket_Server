namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Request
{
    public class ResetPasswordRequest
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string VerifyPassword { get; set; } = string.Empty;
    }
}
