using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class UpdateVoucherRequest
    {
        [StringLength(50, ErrorMessage = "Mã voucher không được vượt quá 50 ký tự")]
        [RegularExpression("^[A-Z0-9]*$", ErrorMessage = "Mã voucher chỉ được chứa chữ in hoa và số")]
        public string? VoucherCode { get; set; }

        [RegularExpression("^(fixed|percent)$", ErrorMessage = "Loại giảm giá phải là 'fixed' hoặc 'percent'")]
        public string? DiscountType { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal? DiscountVal { get; set; }

        public DateOnly? ValidFrom { get; set; }

        public DateOnly? ValidTo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>
        /// false = Public voucher (ai cũng dùng được)
        /// true = Restricted voucher (chỉ user được gửi email mới dùng được)
        /// Lưu ý: Chỉ có thể thay đổi khi voucher chưa được gửi
        /// </summary>
        public bool? IsRestricted { get; set; }
    }
}