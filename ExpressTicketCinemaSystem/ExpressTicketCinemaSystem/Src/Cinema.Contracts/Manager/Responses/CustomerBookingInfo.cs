namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Customer booking information with user basic info
    /// </summary>
    public class CustomerBookingInfo
    {
        // Customer & User IDs
        public int CustomerId { get; set; }
        public int UserId { get; set; }

        // User basic info (chỉ cần: tên, username, phone, email)
        public string? Fullname { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string AvatarUrl { get; set; } = null!;

        // Booking statistics
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalTicketsPurchased { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public DateTime? FirstBookingDate { get; set; }
    }
}

