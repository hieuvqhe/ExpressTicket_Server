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

            // Giờ Việt Nam = UTC + 7
            var currentTime = DateTime.UtcNow.AddHours(7);

            _logger.LogInformation($"Thời gian hiện tại (UTC): {DateTime.UtcNow}");
            _logger.LogInformation($"Thời gian hiện tại (Vietnam - Manual): {currentTime}");

            var expiredShowtimes = await context.Showtimes
                .Where(s => s.Status == "scheduled" &&
                           s.EndTime.HasValue &&
                           s.EndTime.Value <= currentTime)
                .ToListAsync();

            _logger.LogInformation($"Tìm thấy {expiredShowtimes.Count} showtime đã hết hạn");

            if (expiredShowtimes.Any())
            {
                foreach (var showtime in expiredShowtimes)
                {
                    showtime.Status = "finished";
                    showtime.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Đã cập nhật showtime {ShowtimeId} từ 'scheduled' sang 'finished'",
                        showtime.ShowtimeId);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}