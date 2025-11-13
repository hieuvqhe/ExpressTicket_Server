// Application/Services/BookingCheckoutService.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class BookingCheckoutService : IBookingCheckoutService
    {
        private readonly CinemaDbCoreContext _db;

        private static readonly TimeSpan PaymentHoldTtl = TimeSpan.FromMinutes(10); // giữ ghế tới khi thanh toán

        public BookingCheckoutService(CinemaDbCoreContext db)
        {
            _db = db;
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

        public async Task<CheckoutResponse> CheckoutAsync(Guid sessionId, CheckoutRequest req, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var sess = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (sess == null) throw new NotFoundException("Không tìm thấy session");
            if (sess.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (sess.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            // Bắt buộc có ít nhất 1 ghế
            var (seats, _) = ReadItems(sess.ItemsJson);
            if (seats.Count == 0)
                throw new ValidationException("seats", "Bạn cần chọn ít nhất 1 ghế trước khi thanh toán");

            // Kiểm tra ghế vẫn đang lock bởi session
            var anyMissing = await _db.SeatLocks
                .Where(l => l.LockedBySession == sessionId && l.LockedUntil > now && l.ShowtimeId == sess.ShowtimeId)
                .Select(l => l.SeatId)
                .ToListAsync(ct);

            if (!seats.All(sid => anyMissing.Contains(sid)))
                throw new ConflictException("seats", "Một số ghế không còn được giữ. Vui lòng tải lại sơ đồ ghế");

            // Đọc pricing hiện tại
            var pricing = ReadPricing(sess.PricingJson);
            if (pricing.Total <= 0)
                throw new ValidationException("pricing", "Giá trị thanh toán không hợp lệ");

            // Gia hạn locks đến thời hạn payment
            var lockUntil = now.Add(PaymentHoldTtl);
            var locks = await _db.SeatLocks
                .Where(l => l.LockedBySession == sessionId && l.ShowtimeId == sess.ShowtimeId && l.LockedUntil > now)
                .ToListAsync(ct);
            foreach (var l in locks) l.LockedUntil = lockUntil;

            // Tạo Order (đơn giản – bạn có thể thay bằng entity Order thực tế)
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString("N"),
                BookingSessionId = sess.Id,
                ShowtimeId = sess.ShowtimeId,
                Amount = pricing.Total,
                Currency = pricing.Currency,
                Provider = (req.Provider ?? "payos").ToLower(),
                Status = "PENDING",
                CreatedAt = now
            };

            // Chuyển trạng thái session
            sess.State = "PENDING_PAYMENT";
            sess.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            // TODO: tạo payment với PayOS thực → nhận paymentUrl
            string? paymentUrl = null;

            return new CheckoutResponse
            {
                BookingSessionId = sess.Id,
                ShowtimeId = sess.ShowtimeId,
                State = sess.State,
                OrderId = order.OrderId,
                PaymentUrl = paymentUrl,
                ExpiresAt = lockUntil,
                Message = "Khởi tạo thanh toán thành công"
            };
        }

        // Helpers
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
    }
}
