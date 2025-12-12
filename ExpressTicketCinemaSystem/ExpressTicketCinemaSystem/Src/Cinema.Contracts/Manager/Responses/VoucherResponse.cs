namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class VoucherResponse
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
        public bool IsActive { get; set; }
        public bool IsRestricted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Manager info
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = null!;

        // ManagerStaff info
        public int? ManagerStaffId { get; set; }
        public string? ManagerStaffName { get; set; }
    }

    public class VoucherListResponse
    {
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal DiscountVal { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsRestricted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ManagerName { get; set; } = null!;
    }

    public class PaginatedVouchersResponse
    {
        public List<VoucherListResponse> Vouchers { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }

    public class VoucherEmailHistoryResponse
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = null!;
    }

    public class SendVoucherEmailResponse
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<EmailSendResult> Results { get; set; } = new();
    }

    public class EmailSendResult
    {
        public string UserEmail { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }
}