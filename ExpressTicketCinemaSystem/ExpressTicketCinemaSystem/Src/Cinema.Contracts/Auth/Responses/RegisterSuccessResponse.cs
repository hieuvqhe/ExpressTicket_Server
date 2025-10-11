using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses
{
    public class RegisterSuccessResponse
    {
        /// <summary>
        /// Thông báo kết quả đăng ký.
        /// </summary>
        /// <example>Đăng ký thành công. Vui lòng kiểm tra email để xác minh.</example>
        public string Message { get; set; }

        /// <summary>
        /// Thông tin chi tiết của người dùng vừa được tạo.
        /// </summary>
        public object User { get; set; } 
    }
}
