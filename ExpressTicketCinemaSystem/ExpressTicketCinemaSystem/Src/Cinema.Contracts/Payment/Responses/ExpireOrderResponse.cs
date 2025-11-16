namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses
{
    public class ExpireOrderResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // EXPIRED
        public DateTime ExpiredAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


