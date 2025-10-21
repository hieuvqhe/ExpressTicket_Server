using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ManagerService : IManagerService
    {
        private readonly CinemaDbCoreContext _context;

        public ManagerService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<int> GetManagerIdByUserIdAsync(int userId)
        {
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null)
            {
                throw new UnauthorizedException("Người dùng không phải là manager.");
            }

            return manager.ManagerId;
        }

        public async Task<int> GetDefaultManagerIdAsync()
        {
            var manager = await _context.Managers
                .OrderBy(m => m.ManagerId)
                .FirstOrDefaultAsync();

            if (manager == null)
            {
                throw new InvalidOperationException("Không tìm thấy manager trong hệ thống.");
            }

            return manager.ManagerId;
        }

        public async Task<bool> ValidateManagerExistsAsync(int managerId)
        {
            return await _context.Managers
                .AnyAsync(m => m.ManagerId == managerId);
        }

        public async Task<bool> IsUserManagerAsync(int userId)
        {
            return await _context.Managers
                .AnyAsync(m => m.UserId == userId);
        }
    }
}