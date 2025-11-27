namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses
{
    public class CreatePayOSPaymentResponse
    {
        public string OrderId { get; set; } = "";
        public string CheckoutUrl { get; set; } = "";
        public string ProviderRef { get; set; } = "";
        public string QrCode { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }
}



































