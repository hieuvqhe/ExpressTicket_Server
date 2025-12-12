// Contracts/Booking/Requests/ApplyCouponRequest.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class ApplyCouponRequest
    {
        public string VoucherCode { get; set; } = null!;
    }
}

// Contracts/Booking/Requests/CheckoutRequest.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class CheckoutRequest
    {
        // Tạm thời chỉ thông tin payment cơ bản; có thể mở rộng
        public string Provider { get; set; } = "payos";   // "payos"
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}

// Contracts/Booking/Responses/PricingModels.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class PricingBreakdown
    {
        public decimal SeatsSubtotal { get; set; }
        public decimal CombosSubtotal { get; set; }
        public decimal SurchargeSubtotal { get; set; }
        public decimal Fees { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "VND";
    }

    public class ApplyCouponResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public string AppliedVoucher { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public PricingBreakdown Pricing { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class CheckoutResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public string State { get; set; } = "PENDING_PAYMENT";
        public string OrderId { get; set; } = null!;
        public string? PaymentUrl { get; set; }   // link từ PayOS (nếu có)
        public DateTime ExpiresAt { get; set; }   // thời điểm hold ghế đến khi thanh toán
        public string Message { get; set; } = "Khởi tạo thanh toán thành công";
    }
}
