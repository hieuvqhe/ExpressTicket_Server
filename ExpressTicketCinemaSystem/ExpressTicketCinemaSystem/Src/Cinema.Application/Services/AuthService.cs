using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class AuthService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AuthService(CinemaDbCoreContext context, IPasswordHasher<User> passwordHasher, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _config = config;
        }

        // Đăng ký tài khoản
        public async Task<User> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                throw new ArgumentException("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new ArgumentException("Email đã tồn tại.");

            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new Exception("Định dạng email không hợp lệ.");

            if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,12}$"))
                throw new Exception("Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).");

            var user = new User
            {
                Fullname = request.FullName,
                Username = request.Username,
                Email = request.Email,
                UserType = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            user.Password = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = Guid.NewGuid().ToString();
            var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            var emailVerify = new EmailVerificationToken
            {
                UserId = user.UserId,
                TokenHash = tokenBytes,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            await _context.EmailVerificationTokens.AddAsync(emailVerify);
            await _context.SaveChangesAsync();

            await _emailService.SendVerificationEmailAsync(user.Email, token);
            return user;
        }

        // Xác minh email
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
                throw new Exception("Email đã được xác minh.");

            var oldTokens = _context.EmailVerificationTokens.Where(v => v.UserId == user.UserId);
            _context.EmailVerificationTokens.RemoveRange(oldTokens);

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

            await _emailService.SendVerificationEmailAsync(user.Email, newToken);
        }

        // Đăng nhập
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email/Tên đăng nhập và mật khẩu không được để trống.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new UnauthorizedAccessException("Tài khoản chưa được đăng ký.");
            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Email chưa được xác minh. Vui lòng kiểm tra email.");
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Mật khẩu không chính xác.");

            return await CreateJwtResponseAsync(user);
        }

        // Tạo token JWT (dùng chung cho login thường và login Google)
        public async Task<LoginResponse> CreateJwtResponseAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Fullname ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.UserType ?? "User")
        }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

            // Lưu refresh token vào DB
            var refresh = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(refresh);
            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refresh.Token,
                ExpireAt = tokenDescriptor.Expires!.Value,
                FullName = user.Fullname ?? "",
                Role = user.UserType ?? "User"
            };
        }
        public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return null;

            var token = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

            if (token == null) return null;
            if (token.ExpiresAt <= DateTime.UtcNow) return null;
            if (token.User == null || !token.User.IsActive) return null;

            // Thu hồi token cũ, phát hành token mới
            token.IsRevoked = true;

            var newToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = token.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync();

            return await CreateJwtResponseAsync(token.User);
        }
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token == null) return false;

            token.IsRevoked = true;
            await _context.SaveChangesAsync();
            return true;
        }
        // Gửi mã khôi phục (6 số)
        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new ArgumentException("Không tìm thấy tài khoản với email hoặc username này.");
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
                throw new ArgumentException("Vui lòng nhập email hoặc username.");

            if (user != null && user.IsActive == false)
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa, không thể khôi phục mật khẩu.");
            var code = new Random().Next(100000, 999999).ToString();

            var reset = new PasswordResetCode
            {
                UserId = user.UserId,
                Code = code,
                ExpiredAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                IsVerified = false
            };

            _context.PasswordResetCodes.Add(reset);
            await _context.SaveChangesAsync();

            var subject = "Mã khôi phục mật khẩu";
            var body = $"Xin chào {user.Fullname},\n\n" +
                       $"Mã khôi phục mật khẩu của bạn là: {code}\n" +
                       $"Mã có hiệu lực trong 10 phút.\n\n" +
                       $"Nếu bạn không yêu cầu khôi phục, vui lòng bỏ qua email này.";
            await _emailService.SendEmailAsync(user.Email, subject, body);
        }

        //  Xác minh mã OTP
        public async Task VerifyResetCodeAsync(VerifyResetCodeRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new ArgumentException("Không tìm thấy người dùng.");

            var record = await _context.PasswordResetCodes
                .Where(c => c.UserId == user.UserId && c.Code == request.Code && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null || record.ExpiredAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Mã không hợp lệ hoặc đã hết hạn.");
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("Vui lòng nhập mã xác minh.");
            if (record.ExpiredAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Mã xác minh đã hết hạn, vui lòng yêu cầu mã mới.");
            record.IsVerified = true;
            record.VerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        //  Đặt lại mật khẩu
        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.VerifyPassword)
                throw new ArgumentException("Hai mật khẩu không khớp.");

            if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,12}$"))
                throw new ArgumentException("Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new ArgumentException("Không tìm thấy người dùng.");

            var verified = await _context.PasswordResetCodes
                .Where(c => c.UserId == user.UserId && c.IsVerified && !c.IsUsed && c.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(c => c.VerifiedAt)
                .FirstOrDefaultAsync();

            if (verified == null)
                throw new UnauthorizedAccessException("Mã xác minh không hợp lệ hoặc chưa được xác minh.");

            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
            verified.IsUsed = true;

            await _context.SaveChangesAsync();
        }
    }
}
