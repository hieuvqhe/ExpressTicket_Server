namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Response DTO for manager booking statistics
    /// </summary>
    public class ManagerBookingStatisticsResponse
    {
        /// <summary>
        /// Overview statistics
        /// </summary>
        public BookingOverviewStatistics Overview { get; set; } = new BookingOverviewStatistics();

        /// <summary>
        /// Cinema revenue statistics (most important)
        /// </summary>
        public CinemaRevenueStatistics CinemaRevenue { get; set; } = new CinemaRevenueStatistics();

        /// <summary>
        /// Top customers statistics (second most important)
        /// </summary>
        public TopCustomersStatistics TopCustomers { get; set; } = new TopCustomersStatistics();

        /// <summary>
        /// Partner revenue statistics
        /// </summary>
        public PartnerRevenueStatistics PartnerRevenue { get; set; } = new PartnerRevenueStatistics();

        /// <summary>
        /// Movie statistics
        /// </summary>
        public MovieRevenueStatistics MovieStatistics { get; set; } = new MovieRevenueStatistics();

        /// <summary>
        /// Time-based statistics
        /// </summary>
        public TimeBasedStatistics TimeStatistics { get; set; } = new TimeBasedStatistics();

        /// <summary>
        /// Voucher statistics
        /// </summary>
        public VoucherStatistics VoucherStatistics { get; set; } = new VoucherStatistics();

        /// <summary>
        /// Payment statistics
        /// </summary>
        public PaymentStatistics PaymentStatistics { get; set; } = new PaymentStatistics();
    }

    /// <summary>
    /// Overview statistics
    /// </summary>
    public class BookingOverviewStatistics
    {
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalPaidBookings { get; set; }
        public int TotalPendingBookings { get; set; }
        public int TotalCancelledBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, int> BookingsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> RevenueByStatus { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, int> BookingsByPaymentStatus { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Cinema revenue statistics
    /// </summary>
    public class CinemaRevenueStatistics
    {
        public List<CinemaRevenueStat> CinemaRevenueList { get; set; } = new List<CinemaRevenueStat>();
        public List<CinemaRevenueStat> TopCinemasByRevenue { get; set; } = new List<CinemaRevenueStat>();
        public CinemaRevenueComparison? Comparison { get; set; }
        public PaginationMetadata? Pagination { get; set; }
    }

    /// <summary>
    /// Individual cinema revenue statistic
    /// </summary>
    public class CinemaRevenueStat
    {
        public int CinemaId { get; set; }
        public string? CinemaName { get; set; }
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Address { get; set; }
    }

    /// <summary>
    /// Cinema revenue comparison
    /// </summary>
    public class CinemaRevenueComparison
    {
        public CinemaRevenueStat? HighestRevenueCinema { get; set; }
        public CinemaRevenueStat? LowestRevenueCinema { get; set; }
        public decimal AverageRevenuePerCinema { get; set; }
    }

    /// <summary>
    /// Top customers statistics
    /// </summary>
    public class TopCustomersStatistics
    {
        public List<CustomerStat> ByRevenue { get; set; } = new List<CustomerStat>();
        public List<CustomerStat> ByBookingCount { get; set; } = new List<CustomerStat>();
    }

    /// <summary>
    /// Individual customer statistic
    /// </summary>
    public class CustomerStat
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsPurchased { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime? LastBookingDate { get; set; }
    }

    /// <summary>
    /// Partner revenue statistics
    /// </summary>
    public class PartnerRevenueStatistics
    {
        public List<PartnerRevenueStat> PartnerRevenueList { get; set; } = new List<PartnerRevenueStat>();
        public List<PartnerRevenueStat> TopPartnersByRevenue { get; set; } = new List<PartnerRevenueStat>();
        public PaginationMetadata? Pagination { get; set; }
    }

    /// <summary>
    /// Individual partner revenue statistic
    /// </summary>
    public class PartnerRevenueStat
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? TaxCode { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCinemas { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal AverageRevenuePerCinema { get; set; }
    }

    /// <summary>
    /// Movie revenue statistics
    /// </summary>
    public class MovieRevenueStatistics
    {
        public List<MovieRevenueStat> TopMoviesByRevenue { get; set; } = new List<MovieRevenueStat>();
        public List<MovieRevenueStat> TopMoviesByTickets { get; set; } = new List<MovieRevenueStat>();
    }

    /// <summary>
    /// Individual movie revenue statistic
    /// </summary>
    public class MovieRevenueStat
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = null!;
        public string? Genre { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public int ShowtimeCount { get; set; }
    }

    /// <summary>
    /// Time-based statistics
    /// </summary>
    public class TimeBasedStatistics
    {
        public TimePeriodStat Today { get; set; } = new TimePeriodStat();
        public TimePeriodStat Yesterday { get; set; } = new TimePeriodStat();
        public TimePeriodStat ThisWeek { get; set; } = new TimePeriodStat();
        public TimePeriodStat ThisMonth { get; set; } = new TimePeriodStat();
        public TimePeriodStat ThisYear { get; set; } = new TimePeriodStat();
        public List<TimeSeriesData> RevenueTrend { get; set; } = new List<TimeSeriesData>();
        public PeriodComparison? PeriodComparison { get; set; }
    }

    /// <summary>
    /// Time period statistic
    /// </summary>
    public class TimePeriodStat
    {
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
        public int Tickets { get; set; }
        public int Customers { get; set; }
    }

    /// <summary>
    /// Time series data point
    /// </summary>
    public class TimeSeriesData
    {
        public string Date { get; set; } = null!; // yyyy-MM-dd or yyyy-MM or yyyy
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
        public int TicketCount { get; set; }
    }

    /// <summary>
    /// Period comparison
    /// </summary>
    public class PeriodComparison
    {
        public PeriodData CurrentPeriod { get; set; } = new PeriodData();
        public PeriodData PreviousPeriod { get; set; } = new PeriodData();
        public GrowthData Growth { get; set; } = new GrowthData();
    }

    /// <summary>
    /// Period data
    /// </summary>
    public class PeriodData
    {
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
        public int Customers { get; set; }
    }

    /// <summary>
    /// Growth data
    /// </summary>
    public class GrowthData
    {
        public decimal RevenueGrowth { get; set; } // percentage
        public decimal BookingGrowth { get; set; } // percentage
        public decimal CustomerGrowth { get; set; } // percentage
    }

    /// <summary>
    /// Voucher statistics
    /// </summary>
    public class VoucherStatistics
    {
        public int TotalVouchersUsed { get; set; }
        public decimal TotalVoucherDiscount { get; set; }
        public decimal VoucherUsageRate { get; set; } // percentage
        public List<VoucherUsageStat> MostUsedVouchers { get; set; } = new List<VoucherUsageStat>();
    }

    /// <summary>
    /// Individual voucher usage statistic
    /// </summary>
    public class VoucherUsageStat
    {
        public string VoucherCode { get; set; } = null!;
        public int UsageCount { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    /// <summary>
    /// Payment statistics
    /// </summary>
    public class PaymentStatistics
    {
        public List<PaymentProviderStat> PaymentByProvider { get; set; } = new List<PaymentProviderStat>();
        public decimal FailedPaymentRate { get; set; } // percentage
        public decimal PendingPaymentAmount { get; set; }
    }

    /// <summary>
    /// Individual payment provider statistic
    /// </summary>
    public class PaymentProviderStat
    {
        public string? Provider { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

