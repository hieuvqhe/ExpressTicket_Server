using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class BookingPricingService : IBookingPricingService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly IVoucherService _voucherService;

        public BookingPricingService(CinemaDbCoreContext db, IVoucherService voucherService)
        {
            _db = db;
            _voucherService = voucherService;
        }

        private static (List<int> seats, List<int> combos) ReadItems(string? itemsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemsJson)) return (new(), new());
                using var doc = JsonDocument.Parse(itemsJson);
                var root = doc.RootElement;

                var seats = root.TryGetProperty("seats", out var sEl) && sEl.ValueKind == JsonValueKind.Array
                    ? sEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                var combos = root.TryGetProperty("combos", out var cEl) && cEl.ValueKind == JsonValueKind.Array
                    ? cEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                return (seats, combos);
            }
            catch { return (new(), new()); }
        }

        private static PricingBreakdown ReadPricing(string? pricingJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pricingJson)) return new PricingBreakdown();
                return JsonSerializer.Deserialize<PricingBreakdown>(pricingJson!) ?? new PricingBreakdown();
            }
            catch { return new PricingBreakdown(); }
        }

        private static string WritePricing(PricingBreakdown p)
            => JsonSerializer.Serialize(p);

        public async Task<ApplyCouponResponse> ApplyCouponAsync(Guid sessionId, ClaimsPrincipal user, ApplyCouponRequest req, CancellationToken ct = default)
        {
            if (user?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedException("Bạn cần đăng nhập để áp dụng voucher.");

            if (string.IsNullOrWhiteSpace(req.VoucherCode))
                throw new ValidationException("voucherCode", "Mã voucher là bắt buộc");

            var now = DateTime.UtcNow;

            var sess = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (sess == null) throw new NotFoundException("Không tìm thấy session");
            if (sess.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (sess.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            // Tính tạm subtotal hiện tại (đã có ở PricingJson từ preview)
            var pricing = ReadPricing(sess.PricingJson);
            var currentTotalBeforeDiscount = pricing.SeatsSubtotal + pricing.CombosSubtotal + pricing.SurchargeSubtotal + pricing.Fees;

            var vres = await _voucherService.ValidateVoucherForUserAsync(req.VoucherCode.Trim().ToUpper(), currentTotalBeforeDiscount);
            if (!vres.IsValid)
                throw new ValidationException("voucherCode", vres.Message);

            pricing.Discount = vres.DiscountAmount;
            pricing.Total = Math.Max(0, currentTotalBeforeDiscount - pricing.Discount);

            sess.PricingJson = WritePricing(pricing);
            sess.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            return new ApplyCouponResponse
            {
                BookingSessionId = sess.Id,
                ShowtimeId = sess.ShowtimeId,
                AppliedVoucher = req.VoucherCode.Trim().ToUpper(),
                DiscountAmount = vres.DiscountAmount,
                Pricing = pricing,
                ExpiresAt = sess.ExpiresAt
            };
        }
    }
}
