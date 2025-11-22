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
            }

            // 3) Cleanup CANCELED sessions cũ hơn 24 giờ (hard delete)
            var oldCanceledSessions = await context.BookingSessions
                .Where(s => s.State == "CANCELED" && s.UpdatedAt < now.AddHours(-24))
                .ToListAsync();

            if (oldCanceledSessions.Any())
            {
                context.BookingSessions.RemoveRange(oldCanceledSessions);
                _logger.LogInformation("Đã xóa {Count} old CANCELED sessions", oldCanceledSessions.Count);
            }

            if (expiredLocks.Any() || expiredDraftSessions.Any() || oldCanceledSessions.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Cleanup hoàn tất. Đã xử lý {Locks} locks, {Drafts} drafts, {Canceled} canceled",
                    expiredLocks.Count, expiredDraftSessions.Count, oldCanceledSessions.Count);
            }
        }
    }
}























