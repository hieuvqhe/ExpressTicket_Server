using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Requests;
using System.Text.Json;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    /// <summary>
    /// Background service để tự động polling PayOS và xử lý các Order PENDING
    /// Thay thế webhook cho tài khoản PayOS cá nhân không có webhook
    /// </summary>
    public class PaymentPollingService : BackgroundService
    {
        private readonly ILogger<PaymentPollingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30); // Chạy mỗi 30 giây

        public PaymentPollingService(
            ILogger<PaymentPollingService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Polling Service đang chạy (thay thế webhook).");

            // Đợi một chút trước khi bắt đầu lần đầu tiên
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndProcessPendingOrdersAsync(stoppingToken);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi polling payment status");
                    // Đợi 1 phút trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Payment Polling Service đã dừng.");
        }

        private async Task CheckAndProcessPendingOrdersAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();
            var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();

            // Lấy tất cả Order đang PENDING và có PayOsOrderCode
            var pendingOrders = await db.Orders
                .Include(o => o.BookingSession)
                .Where(o => o.Status == "PENDING" 
                    && !string.IsNullOrEmpty(o.PayOsOrderCode)
                    && o.PaymentExpiresAt > DateTime.UtcNow) // Chỉ check các Order chưa hết hạn
                .Take(10) // Giới hạn 10 Order mỗi lần để tránh quá tải
                .ToListAsync(ct);

            if (pendingOrders.Count == 0)
                return;

            _logger.LogInformation("Checking {Count} pending orders...", pendingOrders.Count);

            foreach (var order in pendingOrders)
            {
                try
                {
                    // Gọi PayOS API để check status
                    // PayOS API cần numeric orderCode (convert từ OrderId), không phải paymentLinkId
                    var payOSStatus = await payOSService.GetPaymentStatusAsync(
                        order.OrderId, ct);

                    // Nếu PayOS báo PAID nhưng Order chưa PAID, xử lý như webhook
                    if (payOSStatus.Status == "PAID" && order.Status == "PENDING")
                    {
                        _logger.LogInformation(
                            "Order {OrderId} found PAID in PayOS, processing payment...", 
                            order.OrderId);

                        // Tạo fake webhook request để xử lý
                        var webhookRequest = new PayOSWebhookRequest
                        {
                            Code = "00",
                            Data = new PayOSWebhookData
                            {
                                OrderCode = order.PayOsOrderCode,
                                Amount = (long)order.Amount,
                                Status = "PAID"
                            }
                        };

                        // Gọi logic xử lý payment (giống như webhook)
                        await ProcessSuccessfulPaymentAsync(order, webhookRequest, db, ct);
                    }
                    else if (payOSStatus.Status == "CANCELLED" || payOSStatus.Status == "EXPIRED")
                    {
                        // Order đã bị hủy hoặc hết hạn trên PayOS
                        order.Status = "EXPIRED";
                        order.UpdatedAt = DateTime.UtcNow;

                        // Update booking session state back to DRAFT
                        if (order.BookingSession != null && order.BookingSession.State == "PENDING_PAYMENT")
                        {
                            order.BookingSession.State = "DRAFT";
                            order.BookingSession.UpdatedAt = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync(ct);
                        _logger.LogInformation("Order {OrderId} marked as EXPIRED", order.OrderId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Error checking payment status for Order {OrderId}", 
                        order.OrderId);
                    // Tiếp tục với Order tiếp theo
                }
            }
        }

        private async Task ProcessSuccessfulPaymentAsync(
            Order order,
            PayOSWebhookRequest request,
            CinemaDbCoreContext db,
            CancellationToken ct)
        {
            // Logic này giống hệt trong PaymentController.ProcessSuccessfulPaymentAsync
            // Nhưng vì đây là background service, ta cần copy logic vào đây
            // Hoặc extract thành shared method

            // 1. Update Order status
            order.Status = "PAID";
            order.UpdatedAt = DateTime.UtcNow;

            // 2. Get or create Customer (nếu có UserId)
            int? customerId = null;
            if (order.UserId.HasValue)
            {
                var customer = await db.Customers
                    .FirstOrDefaultAsync(c => c.UserId == order.UserId.Value, ct);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        UserId = order.UserId.Value,
                        LoyaltyPoints = 0
                    };
                    db.Customers.Add(customer);
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Created new Customer {CustomerId} for User {UserId}", 
                        customer.CustomerId, order.UserId.Value);
                }

                customerId = customer.CustomerId;
            }
            else
            {
                _logger.LogWarning("Order {OrderId} has no UserId, cannot create Booking", order.OrderId);
                await db.SaveChangesAsync(ct);
                return;
            }

            // 3. Load BookingSession với items và pricing
            var session = await db.BookingSessions
                .FirstOrDefaultAsync(s => s.Id == order.BookingSessionId, ct);

            if (session == null)
            {
                _logger.LogError("BookingSession {SessionId} not found for Order {OrderId}", 
                    order.BookingSessionId, order.OrderId);
                await db.SaveChangesAsync(ct);
                return;
            }

            // 4. Parse items và pricing
            var (seats, combos) = ReadItems(session.ItemsJson);
            var pricing = ReadPricing(session.PricingJson);

            // 5. Tạo Booking
            var bookingCode = GenerateBookingCode();
            var booking = new Booking
            {
                CustomerId = customerId.Value,
                ShowtimeId = order.ShowtimeId,
                BookingCode = bookingCode,
                BookingTime = DateTime.UtcNow,
                TotalAmount = order.Amount,
                Status = "CONFIRMED",
                OrderCode = order.OrderId,
                SessionId = session.Id,
                PricingSnapshot = session.PricingJson,
                State = "COMPLETED",
                PaymentProvider = order.Provider,
                PaymentTxId = order.PayOsOrderCode,
                PaymentStatus = "PAID",
                VoucherId = await GetVoucherIdFromCouponCode(session.CouponCode, db, ct),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct); // Save để lấy BookingId

            // 6. Tạo Tickets cho mỗi seat
            var showtime = await db.Showtimes
                .Include(s => s.Screen)
                .FirstOrDefaultAsync(s => s.ShowtimeId == order.ShowtimeId, ct);

            if (showtime != null && seats.Count > 0)
            {
                var seatList = await db.Seats
                    .Include(s => s.SeatType)
                    .Where(s => seats.Contains(s.SeatId))
                    .ToListAsync(ct);

                foreach (var seat in seatList)
                {
                    var surcharge = seat.SeatType?.Surcharge ?? 0m;
                    var ticketPrice = showtime.BasePrice + surcharge;

                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        ShowtimeId = order.ShowtimeId,
                        SeatId = seat.SeatId,
                        Price = ticketPrice,
                        Status = "ACTIVE"
                    };
                    db.Tickets.Add(ticket);
                }
            }

            // 7. Tạo ServiceOrders cho mỗi combo
            var comboGroups = combos.GroupBy(c => c).ToList();
            foreach (var group in comboGroups)
            {
                var serviceId = group.Key;
                var quantity = group.Count();

                var service = await db.Services.FirstOrDefaultAsync(s => s.ServiceId == serviceId, ct);
                if (service != null)
                {
                    var serviceOrder = new ServiceOrder
                    {
                        BookingId = booking.BookingId,
                        ServiceId = serviceId,
                        Quantity = quantity,
                        UnitPrice = service.Price
                    };
                    db.ServiceOrders.Add(serviceOrder);
                }
            }

            // 8. Tạo Payment record
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Amount = order.Amount,
                Method = "ONLINE",
                Provider = order.Provider,
                TransactionId = order.PayOsOrderCode ?? order.OrderId,
                Status = "PAID",
                SignatureOk = true,
                PaidAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };
            db.Payments.Add(payment);

            // 9. Update BookingSession state
            session.State = "COMPLETED";
            session.UpdatedAt = DateTime.UtcNow;

            // 10. Release seat locks
            var seatLocks = await db.SeatLocks
                .Where(l => l.LockedBySession == session.Id)
                .ToListAsync(ct);
            db.SeatLocks.RemoveRange(seatLocks);

            // 11. Update Order với BookingId
            order.BookingId = booking.BookingId;

            // 12. Save all changes
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Payment processed successfully - OrderId: {OrderId}, BookingId: {BookingId}, Tickets: {TicketCount}, ServiceOrders: {ServiceOrderCount}",
                order.OrderId, booking.BookingId, seats.Count, comboGroups.Count);
        }

        private static (System.Collections.Generic.List<int> seats, System.Collections.Generic.List<int> combos) ReadItems(string? itemsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemsJson)) return (new(), new());
                using var doc = JsonDocument.Parse(itemsJson);
                var root = doc.RootElement;

                var seats = root.TryGetProperty("seats", out var sEl) && sEl.ValueKind == JsonValueKind.Array
                    ? sEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new System.Collections.Generic.List<int>();

                var combos = root.TryGetProperty("combos", out var cEl) && cEl.ValueKind == JsonValueKind.Array
                    ? cEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new System.Collections.Generic.List<int>();

                return (seats, combos);
            }
            catch { return (new(), new()); }
        }

        private static object ReadPricing(string? pricingJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pricingJson)) return new { };
                return JsonSerializer.Deserialize<object>(pricingJson) ?? new { };
            }
            catch { return new { }; }
        }

        private async Task<int?> GetVoucherIdFromCouponCode(string? couponCode, CinemaDbCoreContext db, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
                return null;

            var voucher = await db.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == couponCode, ct);

            return voucher?.VoucherId;
        }

        private string GenerateBookingCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"BK{timestamp}{random}";
        }
    }
}

