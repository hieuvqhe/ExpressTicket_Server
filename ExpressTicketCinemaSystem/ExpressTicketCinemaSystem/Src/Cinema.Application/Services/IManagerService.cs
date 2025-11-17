using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IManagerService
    {
        Task<int> GetManagerIdByUserIdAsync(int userId);
        Task<int> GetDefaultManagerIdAsync();
        Task<bool> ValidateManagerExistsAsync(int managerId);
        Task<bool> IsUserManagerAsync(int userId);
        Task<ManagerBookingsResponse> GetManagerBookingsAsync(int userId, GetManagerBookingsRequest request);
        Task<BookingDetailResponse> GetBookingDetailAsync(int userId, int bookingId);
    }
}