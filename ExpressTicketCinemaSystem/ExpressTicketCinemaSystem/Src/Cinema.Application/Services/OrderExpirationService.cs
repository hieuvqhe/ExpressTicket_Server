using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    /// <summary>
    /// Background service để tự động expire các Order quá hạn thanh toán
    /// Expire dựa trên payment_expires_at, không phải seat lock
    /// </summary>
    public class OrderExpirationService : BackgroundService
    {
        private readonly ILogger<OrderExpirationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Chạy mỗi 1 phút

        public OrderExpirationService(
            ILogger<OrderExpirationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Expiration Service đang chạy.");

            // Đợi một chút trước khi bắt đầu lần đầu tiên
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpirePendingOrdersAsync(stoppingToken);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi expire orders");
                    // Đợi 1 phút trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Order Expiration Service đã dừng.");
        }

        private async Task ExpirePendingOrdersAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();

            var now = DateTime.UtcNow;

            // Lấy tất cả Order PENDING đã quá hạn thanh toán (dựa trên payment_expires_at)
            var expiredOrders = await db.Orders
                .Include(o => o.BookingSession)
                .Where(o => o.Status == "PENDING"
                    && o.PaymentExpiresAt.HasValue
                    && o.PaymentExpiresAt.Value < now)
                .Take(50) // Giới hạn 50 Order mỗi lần để tránh quá tải
                .ToListAsync(ct);

            if (expiredOrders.Count == 0)
                return;

            _logger.LogInformation("Tìm thấy {Count} Order đã quá hạn thanh toán", expiredOrders.Count);

            foreach (var order in expiredOrders)
            {
                try
                {
                    using var transaction = await db.Database.BeginTransactionAsync(ct);
                    try
                    {
                        // 1. Update Order status
                        order.Status = "EXPIRED";
                        order.UpdatedAt = now;

                        // 2. Update booking session state back to DRAFT (nếu đang PENDING_PAYMENT)
                        if (order.BookingSession != null && order.BookingSession.State == "PENDING_PAYMENT")
                        {
                            order.BookingSession.State = "DRAFT";
                            order.BookingSession.UpdatedAt = now;
                        }

                        // 3. Release seat locks
                        var seatLocks = await db.SeatLocks
                            .Where(l => l.LockedBySession == order.BookingSessionId)
                            .ToListAsync(ct);

                        if (seatLocks.Any())
                        {
                            db.SeatLocks.RemoveRange(seatLocks);
                            _logger.LogInformation("Đã giải phóng {Count} seat locks cho Order {OrderId}",
                                seatLocks.Count, order.OrderId);
                        }

                        await db.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);

                        _logger.LogInformation("Order {OrderId} đã được đánh dấu hết hạn (payment_expires_at: {ExpiresAt})",
                            order.OrderId, order.PaymentExpiresAt);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogError(ex, "Lỗi khi expire Order {OrderId}", order.OrderId);
                        // Tiếp tục với Order tiếp theo
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý Order {OrderId}", order.OrderId);
                    // Tiếp tục với Order tiếp theo
                }
            }
        }
    }
}











