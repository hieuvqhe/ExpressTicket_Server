namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class CreateContractRequest
    {
        public int PartnerId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = "Partnership";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TermsAndConditions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal? MinimumRevenue { get; set; }
    }
}