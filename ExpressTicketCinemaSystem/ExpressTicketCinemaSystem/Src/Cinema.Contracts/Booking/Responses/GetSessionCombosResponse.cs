// ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class SessionComboItem
    {
        public int ServiceId { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; }

        // Auto voucher (nếu user đã đăng nhập)
        public decimal? PriceAfterAutoDiscount { get; set; }
        public string? AutoVoucherCode { get; set; }
    }

    public class GetSessionCombosResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public int PartnerId { get; set; }

        public List<SessionComboItem> Combos { get; set; } = new();

        // Gợi ý cho FE
        public string Currency { get; set; } = "VND";
        public int SelectionLimit { get; set; } = 8;
        public DateTime ServerTime { get; set; }

        // Chỉ có khi user đã đăng nhập
        public List<UserVoucherResponse>? Vouchers { get; set; }
        public UserVoucherResponse? AutoVoucher { get; set; }
    }
}
