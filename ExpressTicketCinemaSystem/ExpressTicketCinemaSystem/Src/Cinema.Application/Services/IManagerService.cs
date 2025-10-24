namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IManagerService
    {
        Task<int> GetManagerIdByUserIdAsync(int userId);
        Task<int> GetDefaultManagerIdAsync();
        Task<bool> ValidateManagerExistsAsync(int managerId);
        Task<bool> IsUserManagerAsync(int userId);
    }
}