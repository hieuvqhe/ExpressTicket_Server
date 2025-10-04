using ExpressTicketCinemaSystem.Models;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class AuthService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly IEmailService _emailService;

        public AuthService(CinemaDbCoreContext context, IPasswordHasher<AppUser> passwordHasher, IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task<AppUser> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra username tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                throw new Exception("Username already exists.");

            // Kiểm tra email tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new Exception("Email already exists.");

            // Kiểm tra email hợp lệ
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new Exception("Invalid email format.");

            // Kiểm tra password mạnh
            if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,12}$"))
                throw new Exception("Password must include uppercase, lowercase, number, and special character (6–12 chars).");

            // Tạo user domain
            var user = new AppUser
            {
                FullName = request.FullName,
                Username = request.Username,
                Email = request.Email,
                UserType = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            user.Password = _passwordHasher.HashPassword(user, request.Password);

            // Lưu user vào DB
            var dbUser = new User
            {
                Fullname = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Password = user.Password,
                Phone = user.Phone,
                UserType = user.UserType,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                EmailConfirmed = user.EmailConfirmed
            };

            _context.Users.Add(dbUser);
            await _context.SaveChangesAsync();

            // Tạo token verify (GUID) và hash nó để lưu vào DB
            var token = Guid.NewGuid().ToString();
            var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            var emailVerify = new EmailVerificationToken
            {
                UserId = dbUser.UserId,
                TokenHash = tokenBytes,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            await _context.EmailVerificationTokens.AddAsync(emailVerify);
            await _context.SaveChangesAsync();

            // Gửi email xác minh
            await _emailService.SendVerificationEmailAsync(user.Email, token);

            return user;
        }

        // Xác minh email qua token
        public async Task<bool> VerifyEmailAsync(string token)
        {
            var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            var verification = await _context.EmailVerificationTokens
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.TokenHash == tokenBytes);

            if (verification == null)
                throw new Exception("Token không hợp lệ.");

            if (verification.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Token đã hết hạn.");

            verification.User.EmailConfirmed = true;
            verification.ConsumedAt = DateTime.UtcNow;

            _context.EmailVerificationTokens.Remove(verification);
            await _context.SaveChangesAsync();

            return true;
        }

        // Gửi lại email xác minh
        public async Task ResendVerificationEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Không tìm thấy người dùng với email này.");

            if (user.EmailConfirmed)
                throw new Exception("Email đã được xác minh trước đó.");

            // Xóa token cũ
            var oldTokens = _context.EmailVerificationTokens.Where(v => v.UserId == user.UserId);
            _context.EmailVerificationTokens.RemoveRange(oldTokens);

            // Tạo token mới
            var newToken = Guid.NewGuid().ToString();
            var newTokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(newToken));

            var newEmailVerify = new EmailVerificationToken
            {
                UserId = user.UserId,
                TokenHash = newTokenBytes,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            await _context.EmailVerificationTokens.AddAsync(newEmailVerify);
            await _context.SaveChangesAsync();

            // Gửi lại email xác minh
            await _emailService.SendVerificationEmailAsync(user.Email, newToken);
        }
    }
}
