namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses
{
    public class UserVoucherResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountVal { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin thêm để hiển thị cho user
        public string DiscountText { get; set; } = null!;
        public bool IsExpired { get; set; }
        public bool IsAvailable { get; set; }
        public int RemainingUses { get; set; }
    }

    public class VoucherValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = null!;
        public UserVoucherResponse? Voucher { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}