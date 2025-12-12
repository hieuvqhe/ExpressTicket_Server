using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Staff.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IStaffService
    {
        Task<StaffProfileResponse> GetStaffProfileAsync(int userId);
        Task<EmployeePermissionsListResponse> GetMyPermissionsAsync(int userId, List<int>? cinemaIds = null);
    }
}

