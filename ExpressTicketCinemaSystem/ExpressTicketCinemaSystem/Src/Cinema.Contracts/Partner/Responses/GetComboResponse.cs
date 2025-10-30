namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Responses
{
    public class GetComboResponse
    {
        public int ComboId { get; set; }
        public int CinemaId { get; set; }
        public string ComboName { get; set; } = null!;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
