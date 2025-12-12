using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models
{
    public partial class Order
    {
        public string OrderId { get; set; } = null!;
        
        public Guid BookingSessionId { get; set; }
        
        public int? UserId { get; set; }
        
        public int ShowtimeId { get; set; }
        
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "VND";
        
        public string Provider { get; set; } = "payos";
        
        public string Status { get; set; } = "PENDING";
        
        public string? PayOsOrderCode { get; set; }
        
        public string? PayOsPaymentLink { get; set; }
        
        public string? PayOsQrCode { get; set; }
        
        public DateTime? PaymentExpiresAt { get; set; }
        
        public string? CustomerName { get; set; }
        
        public string? CustomerPhone { get; set; }
        
        public string? CustomerEmail { get; set; }
        
        public int? BookingId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        public virtual BookingSession BookingSession { get; set; } = null!;
        
        public virtual User? User { get; set; }
        
        public virtual Showtime Showtime { get; set; } = null!;
        
        public virtual Booking? Booking { get; set; }
    }
}
