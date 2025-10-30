namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Responses
{
    public class GetAllComboResponse
    {
        public IEnumerable<GetComboResponse> Combos { get; set; } = new List<GetComboResponse>();
        public int TotalCount { get; set; }
    }
}
