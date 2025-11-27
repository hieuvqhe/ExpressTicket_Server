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
    /// ✅ FIX: Background service để cleanup expired booking sessions và seat locks
    /// Chạy định kỳ để giải phóng tài nguyên và giữ database sạch
    /// </summary>
    public class BookingSessionCleanupService : BackgroundService
    {
        private readonly ILogger<BookingSessionCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Chạy mỗi 5 phút

        public BookingSessionCleanupService(
            ILogger<BookingSessionCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Session Cleanup Service đang chạy.");

            // Đợi một chút trước khi bắt đầu lần đầu tiên
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessionsAndLocksAsync();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cleanup expired sessions và locks");
                    // Đợi 1 phút trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Booking Session Cleanup Service đã dừng.");
        }

        private async Task CleanupExpiredSessionsAndLocksAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();

            var now = DateTime.UtcNow;

            // 1) Cleanup expired seat locks (hết hạn > 1 phút trước để tránh race condition)
            var expiredLocks = await context.SeatLocks
                .Where(l => l.LockedUntil < now.AddMinutes(-1))
                .ToListAsync();

            if (expiredLocks.Any())
            {
                context.SeatLocks.RemoveRange(expiredLocks);
                _logger.LogInformation("Đã xóa {Count} expired seat locks", expiredLocks.Count);
            }

            // 2) Cleanup expired DRAFT sessions (hết hạn > 5 phút trước)
            var expiredDraftSessions = await context.BookingSessions
                .Where(s => s.State == "DRAFT" && s.ExpiresAt < now.AddMinutes(-5))
                .ToListAsync();

            if (expiredDraftSessions.Any())
            {
                // Chuyển state sang CANCELED thay vì xóa (soft delete)
                foreach (var session in expiredDraftSessions)
                {
                    session.State = "CANCELED";
                    session.UpdatedAt = now;
                }
                _logger.LogInformation("Đã cleanup {Count} expired DRAFT sessions", expiredDraftSessions.Count);

                // ✅ Release voucher reservations cho các session đã expire
                var expiredSessionIds = expiredDraftSessions.Select(s => s.Id).ToList();
                var expiredSessionReservations = await context.VoucherReservations
                    .Where(r => expiredSessionIds.Contains(r.SessionId) && r.ReleasedAt == null)
                    .ToListAsync();

                if (expiredSessionReservations.Any())
                {
                    foreach (var reservation in expiredSessionReservations)
                    {
                        reservation.ReleasedAt = now;
                    }
                    _logger.LogInformation("Đã release {Count} voucher reservations cho expired sessions", expiredSessionReservations.Count);
                }
            }

            // 3) Cleanup CANCELED sessions cũ hơn 24 giờ (hard delete)
            // Chỉ xóa các sessions không có Order nào tham chiếu để tránh lỗi foreign key constraint
            var oldCanceledSessions = await context.BookingSessions
                .Where(s => s.State == "CANCELED" && s.UpdatedAt < now.AddHours(-24))
                .ToListAsync();

            if (oldCanceledSessions.Any())
            {
                // Lấy danh sách session IDs có Orders
                var sessionIdsWithOrders = await context.Orders
                    .Where(o => oldCanceledSessions.Select(s => s.Id).Contains(o.BookingSessionId))
                    .Select(o => o.BookingSessionId)
                    .Distinct()
                    .ToListAsync();

                // Chỉ xóa các sessions không có Orders
                var sessionsToDelete = oldCanceledSessions
                    .Where(s => !sessionIdsWithOrders.Contains(s.Id))
                    .ToList();

                if (sessionsToDelete.Any())
                {
                    context.BookingSessions.RemoveRange(sessionsToDelete);
                    _logger.LogInformation("Đã xóa {Count} old CANCELED sessions (bỏ qua {Skipped} sessions có Orders)", 
                        sessionsToDelete.Count, oldCanceledSessions.Count - sessionsToDelete.Count);
                }
                else if (oldCanceledSessions.Any())
                {
                    _logger.LogInformation("Không xóa CANCELED sessions vì tất cả đều có Orders tham chiếu ({Count} sessions)", 
                        oldCanceledSessions.Count);
                }
            }

            // ✅ Cleanup expired voucher reservations (hết hạn > 1 phút trước)
            var expiredReservations = await context.VoucherReservations
                .Where(r => r.ReleasedAt == null && r.ExpiresAt < now.AddMinutes(-1))
                .ToListAsync();

            if (expiredReservations.Any())
            {
                foreach (var reservation in expiredReservations)
                {
                    reservation.ReleasedAt = now;
                }
                _logger.LogInformation("Đã release {Count} expired voucher reservations", expiredReservations.Count);
            }

            if (expiredLocks.Any() || expiredDraftSessions.Any() || oldCanceledSessions.Any() || expiredReservations.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Cleanup hoàn tất. Đã xử lý {Locks} locks, {Drafts} drafts, {Canceled} canceled",
                    expiredLocks.Count, expiredDraftSessions.Count, oldCanceledSessions.Count);
            }
        }
    }
}






























