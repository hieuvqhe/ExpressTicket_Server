using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class PaginatedEmployeesResponse
    {
        public List<EmployeeResponse> Employees { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}

