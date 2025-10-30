namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Responses
{
    public class CreateComboResponse
    {
        public int ServiceId { get; set; }         
        public int CinemaId { get; set; }

        public string ServiceName { get; set; } = null!;

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }


        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
