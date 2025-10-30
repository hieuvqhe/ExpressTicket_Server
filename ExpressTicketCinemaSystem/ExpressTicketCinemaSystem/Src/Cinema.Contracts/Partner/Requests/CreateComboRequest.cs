namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Requests
{
    public class CreateComboRequest
    {
        public int CinemaId { get; set; }
        public int PartnerId { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? ShortDesc { get; set; }
        public bool IsAvailable { get; set; } = true;

        // Danh sách các item trong combo
        public List<ComboItemRequest> Items { get; set; } = new();
    }

    public class ComboItemRequest
    {
        public int ItemServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        public string ComponentKind { get; set; } = "item"; // "item" hoặc "gift"
    }
}
