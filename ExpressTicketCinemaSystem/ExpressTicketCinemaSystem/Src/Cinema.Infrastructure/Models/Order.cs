namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models
{
    public class Order
    {
        public string OrderId { get; set; } = null!;
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Provider { get; set; } = "payos";
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
