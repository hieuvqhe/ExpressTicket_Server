using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests
{
    public class LogoutRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
