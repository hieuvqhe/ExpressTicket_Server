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

    public class SendVoucherToTopBuyersRequest
    {
        [Required(ErrorMessage = "Tiêu đề email là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Subject { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung email là bắt buộc")]
        public string CustomMessage { get; set; } = null!;

        [Required(ErrorMessage = "Số lượng khách hàng top là bắt buộc")]
        [Range(1, 1000, ErrorMessage = "Số lượng khách hàng phải từ 1 đến 1000")]
        public int TopLimit { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lần booking tối thiểu phải lớn hơn 0")]
        public int? MinBookingCount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số ngày phải lớn hơn 0")]
        public int? PeriodDays { get; set; }

        public bool OnlyPaidBookings { get; set; } = true;
    }

    public class SendVoucherToTopSpendersRequest
    {
        [Required(ErrorMessage = "Tiêu đề email là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Subject { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung email là bắt buộc")]
        public string CustomMessage { get; set; } = null!;

        [Required(ErrorMessage = "Số lượng khách hàng top là bắt buộc")]
        [Range(1, 1000, ErrorMessage = "Số lượng khách hàng phải từ 1 đến 1000")]
        public int TopLimit { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng chi tiêu tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinTotalSpent { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số ngày phải lớn hơn 0")]
        public int? PeriodDays { get; set; }

        public bool OnlyPaidBookings { get; set; } = true;
    }
}