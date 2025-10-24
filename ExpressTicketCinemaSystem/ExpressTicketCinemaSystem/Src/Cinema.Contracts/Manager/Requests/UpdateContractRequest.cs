namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class UpdateContractRequest
    {
        public string? ContractNumber { get; set; }
        public string? ContractType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? TermsAndConditions { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? CommissionRate { get; set; }
        public decimal? MinimumRevenue { get; set; }
    }
}
