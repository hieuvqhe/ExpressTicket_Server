using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class PaginatedManagerStaffsResponse
    {
        public List<ManagerStaffResponse> ManagerStaffs { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}








