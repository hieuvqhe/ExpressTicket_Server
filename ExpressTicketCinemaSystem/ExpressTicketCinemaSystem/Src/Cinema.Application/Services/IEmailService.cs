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

        Task SendVoucherEmailAsync(string email, string userName, string voucherCode, string discountType, decimal discountValue, DateOnly validFrom, DateOnly validTo, string subject, string customMessage);

        /// <summary>
        /// Gửi email xác nhận đặt vé thành công kèm thông tin suất chiếu, ghế, combo và mã QR cho từng ghế.
        /// </summary>
        Task SendBookingTicketEmailAsync(
            string email,
            string userName,
            string movieName,
            string cinemaName,
            string roomName,
            string cinemaAddress,
            DateTime showDatetime,
            string seatCodes,          // Ví dụ: "A1, A2"
            string comboSummary,       // Ví dụ: "Combo Bắp Nước x2, Pepsi x1"
            decimal totalAmount,
            string orderCode);
    }
}