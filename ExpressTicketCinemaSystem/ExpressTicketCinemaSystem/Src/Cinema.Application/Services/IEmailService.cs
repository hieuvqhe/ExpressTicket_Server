namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IEmailService
    {
        // Gửi email xác minh tài khoản
        Task SendVerificationEmailAsync(string email, string token);

        // Gửi email chung 
        Task SendEmailAsync(string email, string subject, string body);
    }
}