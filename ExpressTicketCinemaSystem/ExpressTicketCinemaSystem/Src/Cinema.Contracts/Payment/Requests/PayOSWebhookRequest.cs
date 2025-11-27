namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Requests
{
    public class PayOSWebhookRequest
    {
        public string? Code { get; set; }
        public PayOSWebhookData? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PayOSWebhookData
    {
        public string? OrderCode { get; set; }
        public long? Amount { get; set; }
        public string? Description { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Status { get; set; }
    }
}



































