using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using System.Linq.Dynamic.Core;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;

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

            // QUAN TRỌNG: Chỉ lấy các user còn active (IsActive = true)
            query = query.Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email.Contains(search) || u.Fullname.Contains(search) || u.Username.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.UserType.ToLower() == role.ToLower());
            }

            // SỬA LỖI FILTER VERIFY - Logic mới đơn giản
            if (!string.IsNullOrWhiteSpace(verify))
            {
                switch (verify)
                {
                    case "0": // Unverified: email_confirmed = 0
                        query = query.Where(u => !u.EmailConfirmed);
                        break;
                    case "1": // Verified: email_confirmed = 1
                        query = query.Where(u => u.EmailConfirmed);
                        break;
                    case "2": // Banned: is_banned = 1
                        query = query.Where(u => u.IsBanned);
                        break;
                    default:
                        // Nếu giá trị verify không hợp lệ, không filter gì cả
                        break;
                }
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

        public async Task<AdminUserStatsResponse> GetUserStatsAsync(int userId)
        {
            var stats = new AdminUserStatsResponse();
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
                return (false, " Không thể xóa account của chính bạn", null);
            }

            // Tìm user cần xóa
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User không tìm thấy", null);
            }

            // Kiểm tra không được xóa admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Không được cấp quyền để xóa admin khác", null);
            }

            // Kiểm tra user đã bị deactivated chưa
            if (!user.IsActive)
            {
                return (false, "User đã bị deactivate", null);
            }

            // Kiểm tra user có active bookings không
            bool hasActiveBookings = await CheckUserHasActiveBookings(userId);
            if (hasActiveBookings)
            {
                return (false, "Không thể xóa user có active bookings", null);
            }

            // QUAN TRỌNG: Set IsActive = false để không hiển thị trong get all users
            user.IsActive = false;
            user.DeactivatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Xóa user thành công", user);
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

        public async Task<(bool success, string message, User? user)> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return (false, "User not found", null);
            }

            return (true, "Get user successful", user);
        }

        public async Task<(bool success, string message, User? user)> UpdateUserAsync(int userId, AdminUpdateUserRequest request, int currentAdminId)
        {
            // Kiểm tra không được update chính mình
            if (userId == currentAdminId)
            {
                return (false, "Cannot update your own account", null);
            }

            // Tìm user cần update
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            // Kiểm tra không được update admin khác
            if (user.UserType.ToLower() == "admin" && userId != currentAdminId)
            {
                return (false, "Not authorized to update another admin", null);
            }

            // Kiểm tra trùng lặp dữ liệu
            var duplicateCheckResult = await CheckForDuplicatesAsync(userId, request.Email, request.Username, request.Phone);
            if (!duplicateCheckResult.success)
            {
                return (false, duplicateCheckResult.message, null);
            }

            // Cập nhật các trường nếu có giá trị
            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email.Trim();

            if (request.Phone != null)
                user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(request.UserType))
                user.UserType = request.UserType.Trim();

            if (request.Fullname != null)
                user.Fullname = string.IsNullOrWhiteSpace(request.Fullname) ? null : request.Fullname.Trim();

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (request.EmailConfirmed.HasValue)
                user.EmailConfirmed = request.EmailConfirmed.Value;

            if (!string.IsNullOrWhiteSpace(request.Username))
                user.Username = request.Username.Trim();

            if (!string.IsNullOrWhiteSpace(request.AvataUrl))
                user.AvatarUrl = request.AvataUrl.Trim();

            if (request.IsBanned.HasValue)
            {
                user.IsBanned = request.IsBanned.Value;
                if (request.IsBanned.Value)
                {
                    user.BannedAt = DateTime.UtcNow;
                    user.UnbannedAt = null;
                }
                else
                {
                    user.UnbannedAt = DateTime.UtcNow;
                    user.BannedAt = null;
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (true, "Update user success", user);
        }

        private async Task<(bool success, string message)> CheckForDuplicatesAsync(int userId, string? email, string? username, string? phone)
        {
            var existingUsers = await _context.Users
                .Where(u => u.UserId != userId)
                .ToListAsync();

            bool usernameExists = !string.IsNullOrWhiteSpace(username) &&
                                 existingUsers.Any(u => u.Username.ToLower() == username.ToLower());

            bool emailExists = !string.IsNullOrWhiteSpace(email) &&
                              existingUsers.Any(u => u.Email.ToLower() == email.ToLower());

            bool phoneExists = !string.IsNullOrWhiteSpace(phone) &&
                              existingUsers.Any(u => u.Phone != null && u.Phone.ToLower() == phone.ToLower());

            if (usernameExists && emailExists && phoneExists)
            {
                return (false, "Username, email and phone number already exist");
            }
            else if (usernameExists && emailExists)
            {
                return (false, "Username and email already exist");
            }
            else if (usernameExists && phoneExists)
            {
                return (false, "Username and phone number already exist");
            }
            else if (emailExists && phoneExists)
            {
                return (false, "Email and phone number already exist");
            }
            else if (usernameExists)
            {
                return (false, "Username already exists");
            }
            else if (emailExists)
            {
                return (false, "Email already exists");
            }
            else if (phoneExists)
            {
                return (false, "Phone number already exists");
            }

            return (true, string.Empty);
        }
    }
    }