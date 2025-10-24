namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IEmailService
    {
        // Gửi email xác minh tài khoản
        Task SendVerificationEmailAsync(string email, string token);

        // Gửi email chung 
        Task SendEmailAsync(string email, string subject, string body);
        // Gui email dang ky partner 

        Task SendPartnerRegistrationConfirmationAsync(string email, string fullName, string partnerName);
    }
}