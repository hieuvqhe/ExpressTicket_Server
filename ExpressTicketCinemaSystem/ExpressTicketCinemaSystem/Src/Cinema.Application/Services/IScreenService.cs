using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IScreenService
    {
        Task<ScreenResponse> CreateScreenAsync(int cinemaId, CreateScreenRequest request, int partnerId, int userId);
        Task<ScreenResponse> GetScreenByIdAsync(int screenId, int partnerId, int userId);
        Task<PaginatedScreensResponse> GetScreensAsync(int cinemaId, int partnerId, int userId, int page = 1, int limit = 10,
            string? screenType = null, bool? isActive = null, string? sortBy = "screen_name", string? sortOrder = "asc");
        Task<ScreenResponse> UpdateScreenAsync(int screenId, UpdateScreenRequest request, int partnerId, int userId);
        Task<ScreenActionResponse> DeleteScreenAsync(int screenId, int partnerId, int userId);
    }
}