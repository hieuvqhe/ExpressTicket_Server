using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Gửi email xác minh tài khoản
        public async Task SendVerificationEmailAsync(string email, string token)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"];
            var frontendUrl = _config["AppSettings:FrontendUrl"];

            var verifyUrl = $"{frontendUrl}/verify-email?token={token}";
            var subject = "Xác minh tài khoản TicketExpress";

            var htmlContent = $@"
                <p>Xin chào!</p>
                <p>Bạn vừa đăng ký tài khoản TicketExpress.</p>
                <p>Nhấn vào nút dưới đây để xác minh email của bạn:</p>
                <p>
                    <a href='{verifyUrl}' style='
                        display:inline-block;
                        background-color:#1a73e8;
                        color:white;
                        padding:10px 20px;
                        text-decoration:none;
                        border-radius:5px;'>
                        Xác minh tài khoản
                    </a>
                </p>
                <p>Nếu bạn không đăng ký, vui lòng bỏ qua email này.</p>";

            await SendEmailAsync(email, subject, htmlContent);
        }

        //  gửi email chung 
        public async Task SendEmailAsync(string email, string subject, string body)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"];

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Lỗi gửi email: {response.StatusCode}");
        }
    }
}
