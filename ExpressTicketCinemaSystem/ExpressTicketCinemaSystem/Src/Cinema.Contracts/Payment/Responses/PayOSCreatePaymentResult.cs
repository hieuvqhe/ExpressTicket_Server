namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses
{
    public class PayOSCreatePaymentResult
    {
        public string CheckoutUrl { get; set; } = "";
        public string ProviderRef { get; set; } = "";
        public string QrCode { get; set; } = "";
    }
}
