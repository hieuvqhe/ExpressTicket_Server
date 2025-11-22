namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Paginated list of customers with successful bookings
    /// </summary>
    public class PaginatedCustomerList
    {
        public List<CustomerBookingInfo> Customers { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}

