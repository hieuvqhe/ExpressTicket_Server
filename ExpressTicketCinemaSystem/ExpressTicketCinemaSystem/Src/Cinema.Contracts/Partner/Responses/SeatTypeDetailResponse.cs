namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class SeatTypeDetailResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Surcharge { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thống kê sử dụng
        public int TotalSeats { get; set; }
        public int ActiveSeats { get; set; }
        public int InactiveSeats { get; set; }
    }
}