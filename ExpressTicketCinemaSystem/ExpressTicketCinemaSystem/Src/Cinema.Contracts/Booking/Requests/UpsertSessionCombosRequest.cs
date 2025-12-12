// Contracts/Booking/Requests/UpsertSessionCombosRequest.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class UpsertSessionCombosRequest
    {
        // Danh sách combo cần set cho session (replace theo quantity)
        public List<ComboItem> Items { get; set; } = new();

        public class ComboItem
        {
            public int ServiceId { get; set; }
            /// <summary>Quantity >= 0. 0 = bỏ combo đó.</summary>
            public int Quantity { get; set; }
        }
    }
}

// Contracts/Booking/Requests/PricingPreviewRequest.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class PricingPreviewRequest
    {
        /// <summary>Mã voucher tùy chọn (yêu cầu user đã đăng nhập).</summary>
        public string? VoucherCode { get; set; }
    }
}

// Contracts/Booking/Responses/UpsertSessionCombosResponse.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class UpsertSessionCombosResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }

        // Kết quả sau khi áp dụng: dạng gộp theo quantity để FE hiển thị
        public List<ComboQtyItem> Combos { get; set; } = new();
        public int TotalQuantity { get; set; }

        public class ComboQtyItem
        {
            public int ServiceId { get; set; }
            public string Name { get; set; } = null!;
            public string Code { get; set; } = null!;
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public string? ImageUrl { get; set; }
            public bool IsAvailable { get; set; }
        }
    }
}

// Contracts/Booking/Responses/PricingPreviewResponse.cs
namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class PricingPreviewResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public int SeatCount { get; set; }
        public int ComboCount { get; set; }

        public decimal SeatsSubtotal { get; set; }
        public decimal CombosSubtotal { get; set; }

        public string Currency { get; set; } = "VND";

        // Voucher áp dụng (nếu có & hợp lệ)
        public string? AppliedVoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }

        public decimal Total { get; set; }
    }
}
