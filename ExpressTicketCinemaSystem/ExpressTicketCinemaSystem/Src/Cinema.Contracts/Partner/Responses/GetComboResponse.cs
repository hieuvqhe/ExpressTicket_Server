namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Responses
{
    public class GetComboResponse
    {
        public int ServiceId { get; set; }
        public int CinemaId { get; set; }
        public int PartnerId { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ShortDesc { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ComboItemResponse> Items { get; set; } = new();
    }
}
