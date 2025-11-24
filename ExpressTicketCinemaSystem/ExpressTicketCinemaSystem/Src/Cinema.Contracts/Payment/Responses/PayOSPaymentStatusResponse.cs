namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses
{
    public class PayOSPaymentStatusResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // PENDING, PAID, FAILED, EXPIRED
        public string? PaymentLink { get; set; }
        public string? QrCode { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ProviderRef { get; set; } // PayOS order code
    }
}





















