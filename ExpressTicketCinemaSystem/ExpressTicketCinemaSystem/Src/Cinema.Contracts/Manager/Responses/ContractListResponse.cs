namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class ContractListResponse
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CommissionRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Partner information
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerEmail { get; set; } = string.Empty;
        public string PartnerPhone { get; set; } = string.Empty;

        // Manager information
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
    }

    public class PaginatedContractsResponse
    {
        public List<ContractListResponse> Contracts { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}