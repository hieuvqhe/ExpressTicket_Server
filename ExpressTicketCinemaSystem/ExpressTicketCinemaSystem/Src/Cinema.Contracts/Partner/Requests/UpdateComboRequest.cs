namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Requests
{
    public class UpdateComboRequest
    {

        public string ServiceName { get; set; } = null!;


        public decimal Price { get; set; }


        public string? ImageUrl { get; set; }


        public bool IsAvailable { get; set; }
    }
}
