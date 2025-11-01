using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ShowtimeStatusUpdaterService : BackgroundService, IShowtimeStatusUpdaterService
    {
        private readonly ILogger<ShowtimeStatusUpdaterService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Chạy mỗi 1 phút

        public ShowtimeStatusUpdaterService(
            ILogger<ShowtimeStatusUpdaterService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Showtime Status Updater Service đang chạy.");

            // Đợi một chút trước khi bắt đầu lần đầu tiên
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateShowtimeStatusesAsync();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật trạng thái showtime");
                    // Đợi 30 giây trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("Showtime Status Updater Service đã dừng.");
        }

        public async Task UpdateShowtimeStatusesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();

            var currentTime = DateTime.UtcNow;

            // Tìm tất cả showtime có status = "scheduled" và end_time đã qua
            var expiredShowtimes = await context.Showtimes
                .Where(s => s.Status == "scheduled" &&
                           s.EndTime.HasValue &&
                           s.EndTime.Value <= currentTime)
                .ToListAsync();

            if (expiredShowtimes.Any())
            {
                foreach (var showtime in expiredShowtimes)
                {
                    showtime.Status = "finished";
                    showtime.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Đã cập nhật showtime {ShowtimeId} từ 'scheduled' sang 'finished'. EndTime: {EndTime}",
                        showtime.ShowtimeId, showtime.EndTime);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Đã cập nhật {Count} showtime từ 'scheduled' sang 'finished'", expiredShowtimes.Count);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Showtime Status Updater Service đang dừng...");
            await base.StopAsync(cancellationToken);
        }
    }
}