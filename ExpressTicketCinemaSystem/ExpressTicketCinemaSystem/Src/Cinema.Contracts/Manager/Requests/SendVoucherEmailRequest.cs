using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class SendVoucherEmailRequest
    {
        [Required(ErrorMessage = "Tiêu đề email là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Subject { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung email là bắt buộc")]
        public string CustomMessage { get; set; } = null!;

        public List<int>? UserIds { get; set; } // null = gửi cho tất cả users
    }

    public class SendVoucherToAllRequest
    {
        [Required(ErrorMessage = "Tiêu đề email là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Subject { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung email là bắt buộc")]
        public string CustomMessage { get; set; } = null!;
    }
}