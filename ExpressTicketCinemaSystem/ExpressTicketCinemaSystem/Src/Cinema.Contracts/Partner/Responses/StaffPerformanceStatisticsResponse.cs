using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    /// <summary>
    /// Response DTO for Staff Performance Statistics
    /// </summary>
    public class StaffPerformanceStatisticsResponse
    {
        /// <summary>
        /// List of staff performance sorted by revenue (descending)
        /// </summary>
        public List<StaffPerformanceStat> StaffPerformance { get; set; } = new List<StaffPerformanceStat>();

        /// <summary>
        /// Summary statistics
        /// </summary>
        public StaffPerformanceSummary Summary { get; set; } = new StaffPerformanceSummary();
    }

    /// <summary>
    /// Individual staff performance statistic
    /// </summary>
    public class StaffPerformanceStat
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Total number of bookings from cinemas this staff manages
        /// </summary>
        public int TotalBookings { get; set; }
        
        /// <summary>
        /// Total revenue from cinemas this staff manages
        /// </summary>
        public decimal TotalRevenue { get; set; }
        
        /// <summary>
        /// Average booking value
        /// </summary>
        public decimal AverageBookingValue { get; set; }
        
        /// <summary>
        /// Number of cinemas this staff manages
        /// </summary>
        public int CinemaCount { get; set; }
        
        /// <summary>
        /// List of cinema IDs this staff manages
        /// </summary>
        public List<int> CinemaIds { get; set; } = new List<int>();
        
        /// <summary>
        /// List of cinema names this staff manages
        /// </summary>
        public List<string> CinemaNames { get; set; } = new List<string>();
        
        /// <summary>
        /// Total tickets sold from cinemas this staff manages
        /// </summary>
        public int TotalTicketsSold { get; set; }
        
        /// <summary>
        /// Ranking (1-based) among all staff
        /// </summary>
        public int Rank { get; set; }
    }

    /// <summary>
    /// Summary of staff performance
    /// </summary>
    public class StaffPerformanceSummary
    {
        /// <summary>
        /// Total number of staff
        /// </summary>
        public int TotalStaff { get; set; }
        
        /// <summary>
        /// Total bookings from all staff
        /// </summary>
        public int TotalBookings { get; set; }
        
        /// <summary>
        /// Total revenue from all staff
        /// </summary>
        public decimal TotalRevenue { get; set; }
        
        /// <summary>
        /// Average revenue per staff
        /// </summary>
        public decimal AverageRevenuePerStaff { get; set; }
        
        /// <summary>
        /// Best performing staff
        /// </summary>
        public StaffPerformanceStat? BestPerformer { get; set; }
    }
}








































