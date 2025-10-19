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
        public async Task SendPartnerRegistrationConfirmationAsync(string email, string fullName, string partnerName)
        {
            var subject = "Đăng ký đối tác thành công - Đang chờ duyệt";

            var htmlContent = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
                <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
                <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
            </div>
            
            <div style='padding: 30px; background: #f9f9f9;'>
                <h2 style='color: #333; margin-bottom: 20px;'>Đăng ký đối tác thành công!</h2>
                
                <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea;'>
                    <p style='margin-bottom: 10px;'>Xin chào <strong>{fullName}</strong>,</p>
                    <p style='margin-bottom: 15px;'>Chúng tôi đã nhận được đăng ký đối tác cho <strong>{partnerName}</strong>.</p>
                    
                    <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <h4 style='color: #856404; margin: 0 0 10px 0;'>📋 Trạng thái: <strong>ĐANG CHỜ DUYỆT</strong></h4>
                        <p style='margin: 0; color: #856404;'>
                            Đơn đăng ký của bạn đang được đội ngũ quản trị viên xem xét. 
                            Chúng tôi sẽ liên hệ với bạn trong vòng <strong>24-48 giờ</strong>.
                        </p>
                    </div>
                    
                    <h4 style='color: #333; margin-bottom: 10px;'>Tiếp theo sẽ là:</h4>
                    <ul style='color: #555; line-height: 1.6;'>
                        <li>✅ Xác minh thông tin doanh nghiệp</li>
                        <li>✅ Kiểm tra giấy tờ pháp lý</li>
                        <li>✅ Thiết lập hợp đồng hợp tác</li>
                        <li>✅ Kích hoạt tài khoản đối tác</li>
                    </ul>
                </div>
                
                <div style='margin-top: 25px; padding: 15px; background: #e7f3ff; border-radius: 5px;'>
                    <p style='margin: 0; color: #0c5460;'>
                        <strong>📞 Liên hệ hỗ trợ:</strong><br>
                        Email: partner-support@ticketexpress.com<br>
                        Hotline: 1800-1234 (Miễn phí)
                    </p>
                </div>
            </div>
            
            <div style='padding: 20px; text-align: center; background: #333; color: white;'>
                <p style='margin: 0; font-size: 14px;'>
                    © 2024 TicketExpress. All rights reserved.<br>
                    Đây là email tự động, vui lòng không trả lời.
                </p>
            </div>
        </div>";

            await SendEmailAsync(email, subject, htmlContent);
        }
    }
}
