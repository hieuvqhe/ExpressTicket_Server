using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class CreateVoucherRequest
    {
        [Required(ErrorMessage = "Mã voucher là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã voucher không được vượt quá 50 ký tự")]
        [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Mã voucher chỉ được chứa chữ in hoa và số")]
        public string VoucherCode { get; set; } = null!;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [RegularExpression("^(fixed|percent)$", ErrorMessage = "Loại giảm giá phải là 'fixed' hoặc 'percent'")]
        public string DiscountType { get; set; } = "fixed";

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountVal { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu hiệu lực là bắt buộc")]
        public DateOnly ValidFrom { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc hiệu lực là bắt buộc")]
        public DateOnly ValidTo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}