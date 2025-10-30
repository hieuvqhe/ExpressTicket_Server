using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface ISeatLayoutService
    {
        Task<SeatLayoutResponse> GetSeatLayoutAsync(int screenId, int partnerId, int userId);
        Task<ScreenSeatTypesResponse> GetScreenSeatTypesAsync(int screenId, int partnerId, int userId);
        Task<SeatLayoutActionResponse> CreateOrUpdateSeatLayoutAsync(int screenId, CreateSeatLayoutRequest request, int partnerId, int userId);
        Task<SeatActionResponse> UpdateSeatAsync(int screenId, int seatId, UpdateSeatRequest request, int partnerId, int userId);
        Task<BulkSeatActionResponse> BulkUpdateSeatsAsync(int screenId, BulkUpdateSeatsRequest request, int partnerId, int userId);
        Task<SeatLayoutActionResponse> DeleteSeatLayoutAsync(int screenId, int partnerId, int userId);
        Task<SeatActionResponse> DeleteSeatAsync(int screenId, int seatId, int partnerId, int userId);
        Task<BulkSeatActionResponse> BulkDeleteSeatsAsync(int screenId, BulkDeleteSeatsRequest request, int partnerId, int userId);
    }
}