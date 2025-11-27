using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    /// <summary>
    /// Response DTO for Cinema Cluster Statistics
    /// </summary>
    public class CinemaClusterStatisticsResponse
    {
        /// <summary>
        /// List of cinema clusters with statistics
        /// </summary>
        public List<CinemaClusterStat> Clusters { get; set; } = new List<CinemaClusterStat>();

        /// <summary>
        /// Summary statistics
        /// </summary>
        public CinemaClusterSummary Summary { get; set; } = new CinemaClusterSummary();
    }

    /// <summary>
    /// Individual cinema cluster statistic
    /// </summary>
    public class CinemaClusterStat
    {
        /// <summary>
        /// Cluster identifier (e.g., "Hà Nội", "Hải Phòng", or custom cluster name)
        /// </summary>
        public string ClusterName { get; set; } = string.Empty;
        
        /// <summary>
        /// Grouping type: "city", "district", "custom"
        /// </summary>
        public string GroupBy { get; set; } = string.Empty;
        
        /// <summary>
        /// List of cinema IDs in this cluster
        /// </summary>
        public List<int> CinemaIds { get; set; } = new List<int>();
        
        /// <summary>
        /// List of cinema names in this cluster
        /// </summary>
        public List<string> CinemaNames { get; set; } = new List<string>();
        
        /// <summary>
        /// Total number of bookings
        /// </summary>
        public int TotalBookings { get; set; }
        
        /// <summary>
        /// Total revenue
        /// </summary>
        public decimal TotalRevenue { get; set; }
        
        /// <summary>
        /// Average booking value
        /// </summary>
        public decimal AverageBookingValue { get; set; }
        
        /// <summary>
        /// Total tickets sold
        /// </summary>
        public int TotalTicketsSold { get; set; }
        
        /// <summary>
        /// Average occupancy rate (percentage)
        /// </summary>
        public decimal OccupancyRate { get; set; }
        
        /// <summary>
        /// Top movies in this cluster
        /// </summary>
        public List<ClusterMovieStat> TopMovies { get; set; } = new List<ClusterMovieStat>();
        
        /// <summary>
        /// Number of active staff managing this cluster
        /// </summary>
        public int ActiveStaffCount { get; set; }
        
        /// <summary>
        /// Ranking (1-based) among all clusters by revenue
        /// </summary>
        public int Rank { get; set; }
    }

    /// <summary>
    /// Movie statistic within a cluster
    /// </summary>
    public class ClusterMovieStat
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
    }

    /// <summary>
    /// Summary of cinema cluster statistics
    /// </summary>
    public class CinemaClusterSummary
    {
        /// <summary>
        /// Total number of clusters
        /// </summary>
        public int TotalClusters { get; set; }
        
        /// <summary>
        /// Total bookings across all clusters
        /// </summary>
        public int TotalBookings { get; set; }
        
        /// <summary>
        /// Total revenue across all clusters
        /// </summary>
        public decimal TotalRevenue { get; set; }
        
        /// <summary>
        /// Best performing cluster
        /// </summary>
        public CinemaClusterStat? BestCluster { get; set; }
        
        /// <summary>
        /// Average revenue per cluster
        /// </summary>
        public decimal AverageRevenuePerCluster { get; set; }
    }
}















