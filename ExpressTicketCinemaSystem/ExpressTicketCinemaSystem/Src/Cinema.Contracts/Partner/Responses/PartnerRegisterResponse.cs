namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PartnerRegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public int PartnerId { get; set; }
        public string Status { get; set; } = string.Empty; // "pending", "approved", "rejected"
        public DateTime CreatedAt { get; set; }
    }
}
