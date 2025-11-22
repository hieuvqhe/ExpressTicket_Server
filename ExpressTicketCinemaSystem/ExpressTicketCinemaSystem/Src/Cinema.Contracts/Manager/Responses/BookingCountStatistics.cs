namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Statistics about booking counts
    /// </summary>
    public class BookingCountStatistics
    {
        /// <summary>
        /// Maximum number of bookings by a single customer
        /// </summary>
        public int MaxBookings { get; set; }

        /// <summary>
        /// Minimum number of bookings by a single customer (who has at least 1 booking)
        /// </summary>
        public int MinBookings { get; set; }

        /// <summary>
        /// Average number of bookings per customer
        /// </summary>
        public decimal AverageBookings { get; set; }

        /// <summary>
        /// Customer with most bookings (đầy đủ thông tin: tên, username, phone, email)
        /// </summary>
        public CustomerBookingInfo? CustomerWithMostBookings { get; set; }

        /// <summary>
        /// Customer with least bookings (đầy đủ thông tin: tên, username, phone, email)
        /// </summary>
        public CustomerBookingInfo? CustomerWithLeastBookings { get; set; }
    }
}

