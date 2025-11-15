// Application/Services/BookingCheckoutService.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class BookingCheckoutService : IBookingCheckoutService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly ILogger<BookingCheckoutService> _logger;

        private static readonly TimeSpan PaymentHoldTtl = TimeSpan.FromMinutes(15); // giữ ghế tới khi thanh toán (phải >= payment expires time)

        public BookingCheckoutService(CinemaDbCoreContext db, ILogger<BookingCheckoutService> logger)
        {
            _db = db;
            _logger = logger;
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
            _logger.LogInformation("Checkout - Pricing: Total={Total}, Currency={Currency}, SeatsSubtotal={SeatsSubtotal}, CombosSubtotal={CombosSubtotal}",
                pricing.Total, pricing.Currency, pricing.SeatsSubtotal, pricing.CombosSubtotal);
            
            if (pricing.Total <= 0)
            {
                _logger.LogWarning("Checkout failed - Invalid pricing: Total={Total}", pricing.Total);
                throw new ValidationException("pricing", "Giá trị thanh toán không hợp lệ");
            }

            // Gia hạn locks đến thời hạn payment
            var lockUntil = now.Add(PaymentHoldTtl);
            var locks = await _db.SeatLocks
                .Where(l => l.LockedBySession == sessionId && l.ShowtimeId == sess.ShowtimeId && l.LockedUntil > now)
                .ToListAsync(ct);
            foreach (var l in locks) l.LockedUntil = lockUntil;

            // Tạo Order
            var orderId = Guid.NewGuid().ToString("N");
            var currency = string.IsNullOrWhiteSpace(pricing.Currency) ? "VND" : pricing.Currency;
            
            _logger.LogInformation("Checkout - Creating Order: OrderId={OrderId}, SessionId={SessionId}, UserId={UserId}, ShowtimeId={ShowtimeId}, Amount={Amount}, Currency={Currency}",
                orderId, sess.Id, sess.UserId, sess.ShowtimeId, pricing.Total, currency);
            
            var order = new Order
            {
                OrderId = orderId,
                BookingSessionId = sess.Id,
                UserId = sess.UserId, // Có thể null (anonymous)
                ShowtimeId = sess.ShowtimeId,
                Amount = pricing.Total,
                Currency = currency,
                Provider = (req.Provider ?? "payos").ToLower(),
                Status = "PENDING",
                CreatedAt = now
            };

            // Sử dụng transaction để đảm bảo atomicity
            _logger.LogInformation("Checkout - Starting transaction");
            using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Add Order vào DbContext
                _logger.LogInformation("Checkout - Adding Order to DbContext");
                _db.Orders.Add(order);

                // Chuyển trạng thái session
                _logger.LogInformation("Checkout - Updating session state to PENDING_PAYMENT");
                sess.State = "PENDING_PAYMENT";
                sess.UpdatedAt = now;

                _logger.LogInformation("Checkout - Saving changes to database");
                await _db.SaveChangesAsync(ct);
                
                _logger.LogInformation("Checkout - Committing transaction");
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Checkout successful - OrderId: {OrderId}, SessionId: {SessionId}, Amount: {Amount}",
                    order.OrderId, sess.Id, order.Amount);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during checkout - OrderId: {OrderId}, SessionId: {SessionId}, Error: {Error}",
                    order.OrderId, sess.Id, ex.Message);
                
                // Log inner exception nếu có
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerException}, StackTrace: {StackTrace}", 
                        ex.InnerException.Message, ex.InnerException.StackTrace);
                }
                
                try
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogInformation("Checkout - Transaction rolled back");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
                
                throw new Exception($"Lỗi khi lưu Order vào database: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout - OrderId: {OrderId}, SessionId: {SessionId}, Error: {Error}, StackTrace: {StackTrace}",
                    order.OrderId, sess.Id, ex.Message, ex.StackTrace);
                
                try
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogInformation("Checkout - Transaction rolled back");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
                
                throw;
            }

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
