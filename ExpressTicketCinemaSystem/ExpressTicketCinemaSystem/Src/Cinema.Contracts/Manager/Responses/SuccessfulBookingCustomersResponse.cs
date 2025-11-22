namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Response DTO for customers with successful bookings
    /// </summary>
    public class SuccessfulBookingCustomersResponse
    {
        /// <summary>
        /// Top customers sorted by booking count
        /// </summary>
        public List<CustomerBookingInfo> TopCustomersByBookingCount { get; set; } = new();

        /// <summary>
        /// Top customers sorted by total spent
        /// </summary>
        public List<CustomerBookingInfo> TopCustomersByTotalSpent { get; set; } = new();

        /// <summary>
        /// Full paginated list of all customers with successful bookings
        /// </summary>
        public PaginatedCustomerList FullCustomerList { get; set; } = new();

        /// <summary>
        /// Statistics about booking counts
        /// </summary>
        public BookingCountStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Total number of unique customers with successful bookings
        /// </summary>
        public int TotalCustomers { get; set; }
    }
}

