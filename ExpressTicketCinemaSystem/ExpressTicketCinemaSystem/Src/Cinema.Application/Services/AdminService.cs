using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using System.Linq.Dynamic.Core;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class AdminService
    {
        private readonly CinemaDbCoreContext _context;

        public AdminService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<(List<User>, int)> GetFilteredUsersAsync(
            int page, int limit, string? search, string? role, string? verify,
            string sortBy = "created_at", string sortOrder = "desc")
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email.Contains(search) || u.Fullname.Contains(search) || u.Username.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.UserType.ToLower() == role.ToLower());
            }

            if (verify == "0")
            {
                query = query.Where(u => !u.EmailConfirmed);
            }
            else if (verify == "1")
            {
                query = query.Where(u => u.EmailConfirmed && u.IsActive && !u.IsBanned);
            }
            else if (verify == "2")
            {
                query = query.Where(u => u.IsBanned);
            }

            var total = await query.CountAsync();

            sortBy = sortBy.ToLower() switch
            {
                "created_at" => "CreatedAt",
                "email" => "Email",
                "fullname" => "Fullname",
                "username" => "Username",
                "user_type" => "UserType",
                _ => "CreatedAt"
            };

            var validSortOrder = sortOrder?.ToLower() == "asc" ? "ascending" : "descending";
            query = query.OrderBy($"{sortBy} {validSortOrder}");

            var users = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (users, total);
        }

        public async Task<UserStatsResponse> GetUserStatsAsync(int userId)
        {
            var stats = new UserStatsResponse();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return stats;

            var customerId = customer.CustomerId;
            stats.BookingsCount = await _context.Bookings.CountAsync(b => b.CustomerId == customerId);
            stats.RatingsCount = await _context.RatingFilms.CountAsync(r => r.UserId == customerId);
            stats.CommentsCount = await _context.RatingFilms.CountAsync(r => r.UserId == customerId && r.Comment != null);

            return stats;
        }

        public async Task<(bool success, string message, User? user)> SoftDeleteUserAsync(int userId, int currentAdminId)
        {
            // Kiểm tra không được xóa chính mình
            if (userId == currentAdminId)
            {
                return (false, "Cannot delete your own account", null);
            }

            // Tìm user cần xóa
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            // Kiểm tra không được xóa admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Not authorized to delete another admin", null);
            }

            // Kiểm tra user đã bị deactivated chưa
            if (!user.IsActive)
            {
                return (false, "User is already deactivated", null);
            }

            // Kiểm tra user có active bookings không
            bool hasActiveBookings = await CheckUserHasActiveBookings(userId);
            if (hasActiveBookings)
            {
                return (false, "Cannot delete user with active bookings", null);
            }

            user.IsActive = false;
            user.DeactivatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Delete user success", user);
        }

        private async Task<bool> CheckUserHasActiveBookings(int userId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return false;

            return await _context.Bookings
                .AnyAsync(b => b.CustomerId == customer.CustomerId &&
                              (b.Status == "pending" || b.Status == "confirmed"));
        }
    

    public async Task<(bool success, string message, User? user)> BanUserAsync(int userId, int currentAdminId)
        {
            // Kiểm tra không được ban chính mình
            if (userId == currentAdminId)
            {
                return (false, "Cannot ban your own account", null);
            }

            // Tìm user cần ban
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            // Kiểm tra không được ban admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Not authorized to ban another admin", null);
            }

            // Kiểm tra user đã bị banned chưa
            if (user.IsBanned)
            {
                return (false, "User already banned", null);
            }

            // Thực hiện ban user
            user.IsBanned = true;
            user.BannedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Ban user success", user);
        }

        public async Task<(bool success, string message, User? user)> UnbanUserAsync(int userId, int currentAdminId)
        {
            // Kiểm tra không được unban chính mình
            if (userId == currentAdminId)
            {
                return (false, "Cannot unban your own account", null);
            }

            // Tìm user cần unban
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            // Kiểm tra không được unban admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Not authorized to unban another admin", null);
            }

            // Kiểm tra user có bị banned không
            if (!user.IsBanned)
            {
                return (false, "User is not banned", null);
            }

            // Thực hiện unban user
            user.IsBanned = false;
            user.UnbannedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Unban user success", user);
        }

        public async Task<(bool success, string message, User? user)> UpdateUserRoleAsync(int userId, string newRole, int currentAdminId)
        {
            // Kiểm tra không được update role của chính mình
            if (userId == currentAdminId)
            {
                return (false, "Cannot update your own role", null);
            }

            // Tìm user cần update
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            // Kiểm tra không được update role của admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Not authorized to update another admin's role", null);
            }

            // Kiểm tra user có bị banned không (nếu cần thiết)
            if (user.IsBanned)
            {
                return (false, "Cannot update role for banned user", null);
            }

            // Thực hiện update role
            user.UserType = newRole.ToLower();
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Update user role success", user);
        }
    }
    }