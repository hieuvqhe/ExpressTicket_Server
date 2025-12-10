using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Helpers
{
    /// <summary>
    /// Helper class để xử lý thời gian theo múi giờ Việt Nam (UTC+7)
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Lấy thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime NowVN() => DateTime.UtcNow.AddHours(7);

        /// <summary>
        /// Lấy ngày hiện tại theo múi giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateOnly TodayVN() => DateOnly.FromDateTime(NowVN());

        /// <summary>
        /// Chuyển đổi DateTime từ UTC sang giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            return utcDateTime.AddHours(7);
        }

        /// <summary>
        /// Chuyển đổi DateTime? từ UTC sang giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime? ToVietnamTime(DateTime? utcDateTime)
        {
            return utcDateTime?.AddHours(7);
        }
    }
}

