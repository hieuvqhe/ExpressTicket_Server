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

        // Gửi email voucher - THÊM MỚI
        public async Task SendVoucherEmailAsync(string email, string userName, string voucherCode, string discountType, decimal discountValue, DateOnly validFrom, DateOnly validTo, string subject, string customMessage)
        {
            var discountText = discountType == "percent"
                ? $"{discountValue}%"
                : $"{discountValue:N0} VNĐ";

            var htmlContent = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
                <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
                <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
            </div>
            
            <div style='padding: 30px; background: #f9f9f9;'>
                <h2 style='color: #333; margin-bottom: 20px;'>🎉 Ưu Đãi Đặc Biệt</h2>
                
                <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #667eea;'>
                    <p style='margin-bottom: 15px;'>Xin chào <strong>{userName}</strong>,</p>
                    <p style='margin-bottom: 20px;'>{customMessage}</p>
                    
                    <div style='background: #f8f9fa; border: 2px dashed #667eea; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px;'>
                        <h3 style='color: #667eea; margin: 0 0 10px 0; font-size: 18px;'>MÃ VOUCHER CỦA BẠN</h3>
                        <div style='font-size: 28px; font-weight: bold; color: #667eea; letter-spacing: 2px;'>{voucherCode}</div>
                    </div>
                    
                    <div style='background: #e7f3ff; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <h4 style='color: #0c5460; margin: 0 0 15px 0;'>📊 Thông tin giảm giá</h4>
                        <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 10px;'>
                            <div>
                                <strong>Giá trị:</strong><br>
                                <span style='font-size: 18px; color: #28a745;'>{discountText}</span>
                            </div>
                            <div>
                                <strong>Thời hạn:</strong><br>
                                {validFrom:dd/MM/yyyy} - {validTo:dd/MM/yyyy}
                            </div>
                        </div>
                    </div>
                    
                    <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <h4 style='color: #856404; margin: 0 0 10px 0;'>📝 Hướng dẫn sử dụng</h4>
                        <ol style='color: #856404; line-height: 1.6; margin: 0; padding-left: 20px;'>
                            <li>Chọn vé xem phim và dịch vụ mong muốn</li>
                            <li>Nhập mã <strong>{voucherCode}</strong> tại bước thanh toán</li>
                            <li>Hệ thống sẽ tự động áp dụng giảm giá</li>
                        </ol>
                    </div>
                    
                    <p style='text-align: center; margin: 20px 0 0 0;'>
                        <strong>Hãy nhanh tay sử dụng voucher trước khi hết hạn!</strong>
                    </p>
                </div>
                
                <div style='margin-top: 25px; padding: 15px; background: #e7f3ff; border-radius: 5px;'>
                    <p style='margin: 0; color: #0c5460;'>
                        <strong>📞 Liên hệ hỗ trợ:</strong><br>
                        Email: support@ticketexpress.com<br>
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

        // Gửi email vé xem phim sau khi thanh toán thành công
        public async Task SendBookingTicketEmailAsync(
            string email,
            string userName,
            string movieName,
            string cinemaName,
            string roomName,
            string cinemaAddress,
            DateTime showDatetime,
            string seatCodes,
            string comboSummary,
            decimal totalAmount,
            string orderCode)
        {
            var when = showDatetime.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
            var seats = string.IsNullOrWhiteSpace(seatCodes) ? "(đang cập nhật)" : seatCodes;
            var user = string.IsNullOrWhiteSpace(userName) ? "bạn" : userName;

            // Parse danh sách ghế: "A1,A2" -> ["A1", "A2"]
            var seatList = seats
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // Tạo QR code cho từng ghế và đính kèm dưới dạng file (attachment)
            string qrCodeHtml;
            if (seatList.Count == 0)
            {
                qrCodeHtml = "<div style='margin: 20px 0; text-align: center; padding: 15px; background: #fff3cd; border: 1px solid #ffc107; border-radius: 5px;'>"
                           + "<p style='color: #856404;'>Không có thông tin ghế</p>"
                           + "</div>";
            }
            else
            {
                // Thông báo hướng dẫn người dùng tải QR ở phần file đính kèm
                qrCodeHtml = "<div style='margin: 20px 0; padding: 15px; background: #e7f3ff; border: 1px solid #90cdf4; border-radius: 5px;'>"
                           + "<p style='margin: 0; color: #0c5460; font-size: 13px;'>"
                           + "Mã QR Code vé của bạn được đính kèm ở phía dưới email dưới dạng file ảnh. "
                           + "Mỗi ghế tương ứng với một file QR riêng, bạn có thể tải xuống và trình mã này khi vào rạp."
                           + "</p></div>";

                // Danh sách attachment QR để thêm vào mail
                var qrAttachments = new List<Attachment>();

                for (int idx = 0; idx < seatList.Count; idx++)
                {
                    var seat = seatList[idx];
                    var ticketIdentifier = $"{seat}{orderCode}";

                    // Tạo ảnh QR bằng external service rồi convert sang base64 để attach inline
                    var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(ticketIdentifier)}";
                    try
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(qrUrl);
                        var base64 = Convert.ToBase64String(bytes);

                        // Đặt tên file không lộ seat/orderId (ticket_qr_1.png, ticket_qr_2.png, ...)
                        qrAttachments.Add(new Attachment
                        {
                            Content = base64,
                            Type = "image/png",
                            Filename = $"ticket_qr_{idx + 1}.png",
                            Disposition = "attachment"
                        });
                    }
                    catch
                    {
                        // Nếu tạo QR thất bại, bỏ qua file QR tương ứng
                    }
                }

                var totalMoneyText = $"{totalAmount:N0} VND";
                var combosDisplay = string.IsNullOrWhiteSpace(comboSummary) ? "Không có combo" : comboSummary;

                var htmlContent = "<html><body style='font-family:Arial,sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>" +
                                  "<div style='background: #f5f5f5; padding: 20px; border-radius: 10px;'>" +
                                  "<h2 style='color: #2c3e50; margin-top: 0;'>🎟️ Xác nhận đặt vé thành công</h2>" +
                                  $"<p>Xin chào <b>{Escape(user)}</b>,</p>" +
                                  "<p>Đơn hàng của bạn đã thanh toán thành công.</p>" +
                                  "<div style='background: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>" +
                                  "<ul style='list-style: none; padding: 0; margin: 0;'>" +
                                  $"<li style='margin: 8px 0;'><strong>Phim:</strong> {Escape(movieName ?? string.Empty)}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Rạp / Phòng chiếu:</strong> {Escape(cinemaName ?? string.Empty)} - {Escape(roomName ?? string.Empty)}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Địa chỉ:</strong> {Escape(cinemaAddress ?? string.Empty)}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Suất chiếu:</strong> {when}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Ghế:</strong> {Escape(seats)}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Combo:</strong> {Escape(combosDisplay)}</li>" +
                                  $"<li style='margin: 8px 0;'><strong>Tổng tiền:</strong> {totalMoneyText}</li>" +
                                  "</ul>" +
                                  "</div>" +
                                  qrCodeHtml +
                                  "<p style='margin-top: 20px; color: #666;'>Cảm ơn bạn đã đặt vé tại TicketExpress.</p>" +
                                  "</div>" +
                                  "</body></html>";

                var subject = $"Xác nhận đặt vé thành công - Đơn hàng {orderCode}";

                // Tạo mail với inline attachments (CID)
                var apiKey = _config["SendGrid:ApiKey"];
                var client = new SendGridClient(apiKey);
                var fromEmail = _config["SendGrid:FromEmail"];
                var fromName = _config["SendGrid:FromName"];
                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(email);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
                if (qrAttachments.Count > 0)
                {
                    msg.AddAttachments(qrAttachments);
                }

                var response = await client.SendEmailAsync(msg);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Lỗi gửi email vé: {response.StatusCode}");

                return;
            }

            // Trường hợp không có ghế, fallback gửi mail thường (không có QR)
            var totalMoneyTextFallback = $"{totalAmount:N0} VND";
            var combosDisplayFallback = string.IsNullOrWhiteSpace(comboSummary) ? "Không có combo" : comboSummary;

            var htmlFallback = "<html><body style='font-family:Arial,sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>" +
                               "<div style='background: #f5f5f5; padding: 20px; border-radius: 10px;'>" +
                               "<h2 style='color: #2c3e50; margin-top: 0;'>🎟️ Xác nhận đặt vé thành công</h2>" +
                               $"<p>Xin chào <b>{Escape(user)}</b>,</p>" +
                               "<p>Đơn hàng của bạn đã thanh toán thành công.</p>" +
                               "<div style='background: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>" +
                               "<ul style='list-style: none; padding: 0; margin: 0;'>" +
                               $"<li style='margin: 8px 0;'><strong>Phim:</strong> {Escape(movieName ?? string.Empty)}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Rạp / Phòng chiếu:</strong> {Escape(cinemaName ?? string.Empty)} - {Escape(roomName ?? string.Empty)}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Địa chỉ:</strong> {Escape(cinemaAddress ?? string.Empty)}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Suất chiếu:</strong> {when}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Ghế:</strong> {Escape(seats)}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Combo:</strong> {Escape(combosDisplayFallback)}</li>" +
                               $"<li style='margin: 8px 0;'><strong>Tổng tiền:</strong> {totalMoneyTextFallback}</li>" +
                               "</ul>" +
                               "</div>" +
                               qrCodeHtml +
                               "<p style='margin-top: 20px; color: #666;'>Cảm ơn bạn đã đặt vé tại TicketExpress.</p>" +
                               "</div>" +
                               "</body></html>";

            var subjectFallback = $"Xác nhận đặt vé thành công - Đơn hàng {orderCode}";
            await SendEmailAsync(email, subjectFallback, htmlFallback);
        }

        private static string Escape(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#x27;");
        }

    }
}
