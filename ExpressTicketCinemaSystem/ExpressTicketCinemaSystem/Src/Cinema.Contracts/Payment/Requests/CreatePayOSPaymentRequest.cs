namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Requests
{
    public class CreatePayOSPaymentRequest
    {
        public string OrderId { get; set; } = "";
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
