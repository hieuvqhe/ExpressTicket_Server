using System.Collections.Generic;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class PartnerWithoutContractResponse
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
    }

    public class PaginatedPartnersWithoutContractsResponse
    {
        public List<PartnerWithoutContractResponse> Partners { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}