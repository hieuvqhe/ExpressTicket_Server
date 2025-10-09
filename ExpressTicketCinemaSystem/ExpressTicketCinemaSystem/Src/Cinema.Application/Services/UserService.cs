using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class UserService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;

        // Configurable rules
        private const int CodeExpiryMinutes = 10;
        private const int MaxCodeAttempts = 5; // optional if you extend DB to store Attempts

        public UserService(CinemaDbCoreContext context, IConfiguration config, IEmailService emailService, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserProfileResponse?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var defaultAvatar = _config["Defaults:AvatarUrl"] ??
                "https://tse2.mm.bing.net/th/id/OIP.Ai9h_6D7ojZdsZnE4_6SDgAAAA?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3";

            return new UserProfileResponse
            {
                UserId = user.UserId,
                Fullname = user.Fullname ?? string.Empty,
                Username = user.Username ?? string.Empty,
                Password = "********",
                Phone = user.Phone,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? defaultAvatar : user.AvatarUrl,
                Email = user.Email ?? string.Empty
            };
        }

        public async Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var errors = new List<string>();

            if (request.Fullname != null)
            {
                var fullnameTrim = request.Fullname.Trim();
                if (string.IsNullOrEmpty(fullnameTrim))
                    errors.Add("Fullname không được rỗng.");
                else if (fullnameTrim.Length > 255)
                    errors.Add("Fullname quá dài (tối đa 255 ký tự).");
            }

            if (request.Phone != null)
            {
                var phone = request.Phone.Trim();
                if (!Regex.IsMatch(phone, @"^0\d{9}$"))
                    errors.Add("Phone phải có đúng 10 chữ số và bắt đầu bằng '0'.");
            }

            if (request.AvatarUrl != null && request.AvatarUrl.Length > 2000)
            {
                errors.Add("AvatarUrl quá dài.");
            }

            if (errors.Any())
                throw new InvalidOperationException(string.Join(" | ", errors));

            var changed = false;
            if (request.Fullname != null && request.Fullname.Trim() != user.Fullname)
            {
                user.Fullname = request.Fullname.Trim();
                changed = true;
            }
            if (request.Phone != null && request.Phone.Trim() != user.Phone)
            {
                user.Phone = request.Phone.Trim();
                changed = true;
            }
            if (request.AvatarUrl != null && request.AvatarUrl.Trim() != user.AvatarUrl)
            {
                user.AvatarUrl = request.AvatarUrl.Trim();
                changed = true;
            }

            if (changed)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            var defaultAvatar = _config["Defaults:AvatarUrl"] ??
                "https://tse2.mm.bing.net/th/id/OIP.Ai9h_6D7ojZdsZnE4_6SDgAAAA?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3";

            return new UserProfileResponse
            {
                UserId = user.UserId,
                Fullname = user.Fullname ?? string.Empty,
                Username = user.Username ?? string.Empty,
                Password = "********",
                Phone = user.Phone,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? defaultAvatar : user.AvatarUrl,
                Email = user.Email ?? string.Empty
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new ArgumentException("Vui lòng cung cấp mật khẩu cũ, mật khẩu mới và xác nhận mật khẩu.");
            }

            if (request.NewPassword != request.ConfirmPassword)
                throw new ArgumentException("Mật khẩu mới và xác nhận mật khẩu không khớp.");

            var pwdPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,12}$";
            if (!Regex.IsMatch(request.NewPassword, pwdPattern))
                throw new ArgumentException("Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Mật khẩu cũ không chính xác.");

            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);

            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var t in tokens)
                t.IsRevoked = true;

            await _context.SaveChangesAsync();
        }

        private static string GenerateSixDigitCode()
        {
            var rnd = RandomNumberGenerator.GetInt32(0, 1000000);
            return rnd.ToString("D6");
        }

        private static byte[] HashCode(string code)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(code));
        }

        private static bool IsExpired(DateTime? expiresAt) =>
            !expiresAt.HasValue || expiresAt.Value < DateTime.UtcNow;

        public async Task<RequestEmailChangeResponse> RequestEmailChangeAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            if (string.IsNullOrWhiteSpace(user.Email)) throw new InvalidOperationException("User has no email configured.");

            // Check existing active request (not consumed)
            var existing = await _context.EmailChangeRequests
                .Where(r => r.UserId == userId && !r.IsConsumed)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null && !IsExpired(existing.CurrentCodeExpiresAt))
            {
                // If current code still valid and not verified, return existing request id to avoid spamming emails.
                if (!existing.CurrentVerified)
                {
                    return new RequestEmailChangeResponse
                    {
                        RequestId = existing.RequestId,
                        ExpiresAt = existing.CurrentCodeExpiresAt!.Value,
                        CurrentVerified = existing.CurrentVerified
                    };
                }
                // If current already verified but new part pending, also return it
                if (!existing.NewVerified)
                {
                    return new RequestEmailChangeResponse
                    {
                        RequestId = existing.RequestId,
                        ExpiresAt = existing.NewCodeExpiresAt ?? existing.CurrentCodeExpiresAt!.Value,
                        CurrentVerified = existing.CurrentVerified
                    };
                }
            }

            // create new request (no NewEmail yet)
            var requestId = Guid.NewGuid();
            var currentCode = GenerateSixDigitCode();

            var now = DateTime.UtcNow;
            var currentExpires = now.AddMinutes(CodeExpiryMinutes);

            var changeReq = new EmailChangeRequest
            {
                RequestId = requestId,
                UserId = userId,
                NewEmail = null, 
                CurrentCodeHash = HashCode(currentCode),
                NewCodeHash = null,
                CurrentCodeExpiresAt = currentExpires,
                NewCodeExpiresAt = null,
                CurrentVerified = false,
                NewVerified = false,
                IsConsumed = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.EmailChangeRequests.Add(changeReq);
            await _context.SaveChangesAsync();

            // send code to current email
            var subject = "Xác nhận thay đổi email - mã xác thực";
            var body = $"Xin chào {user.Fullname},\n\n" +
                       $"Bạn vừa yêu cầu thay đổi email cho tài khoản. Mã xác thực (gửi tới email hiện tại) là: {currentCode}\n" +
                       $"Mã có hiệu lực trong {CodeExpiryMinutes} phút.\n\n" +
                       "Nếu bạn không yêu cầu, vui lòng bỏ qua email này.";

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            return new RequestEmailChangeResponse
            {
                RequestId = requestId,
                ExpiresAt = currentExpires,
                CurrentVerified = false
            };
        }

        public async Task VerifyCurrentEmailCodeAsync(int userId, Guid requestId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null)
                throw new KeyNotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            // Defensive: check that we actually have a stored code hash
            if (req.CurrentCodeHash == null || req.CurrentCodeHash.Length == 0)
            {
                // mark consumed to avoid reuse & give a clear message
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Không tìm thấy mã hiện tại trên hệ thống — vui lòng tạo lại yêu cầu đổi email.");
            }

            if (IsExpired(req.CurrentCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Mã xác thực hiện tại đã hết hạn.");
            }

            var hashed = HashCode(code);

            // Compare safely
            if (!hashed.SequenceEqual(req.CurrentCodeHash))
            {
                // optionally: increment attempts here (if you add Attempts column)
                throw new UnauthorizedAccessException("Mã xác thực không hợp lệ.");
            }

            req.CurrentVerified = true;
            req.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        public async Task<RequestEmailChangeResponse> SubmitNewEmailAsync(int userId, SubmitNewEmailRequest model)
        {
            if (model == null) throw new ArgumentException("Request body null.");
            if (model.RequestId == Guid.Empty) throw new ArgumentException("Missing requestId.");
            if (string.IsNullOrWhiteSpace(model.NewEmail)) throw new ArgumentException("NewEmail không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId && r.UserId == userId && !r.IsConsumed);

            if (req == null) throw new KeyNotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (!req.CurrentVerified) throw new UnauthorizedAccessException("Chưa xác thực email hiện tại.");

            if (IsExpired(req.CurrentCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Yêu cầu đã hết hạn. Vui lòng tạo lại yêu cầu.");
            }

            var newEmail = model.NewEmail.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Định dạng email mới không hợp lệ.");

            var exists = await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (exists) throw new ArgumentException("Email mới đã được sử dụng bởi tài khoản khác.");

            // generate and send new code
            var newCode = GenerateSixDigitCode();
            req.NewEmail = newEmail;
            req.NewCodeHash = HashCode(newCode);
            req.NewCodeExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes);
            req.NewVerified = false;
            req.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var subject = "Xác minh email mới - mã xác thực";
            var body = $"Xin chào,\n\n" +
                       $"Bạn (hoặc ai đó) đang cố gắng liên kết email này với tài khoản. Mã xác thực để xác minh email mới là: {newCode}\n" +
                       $"Mã có hiệu lực trong {CodeExpiryMinutes} phút.\n\n" +
                       "Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.";

            await _emailService.SendEmailAsync(newEmail, subject, body);

            return new RequestEmailChangeResponse
            {
                RequestId = req.RequestId,
                ExpiresAt = req.NewCodeExpiresAt!.Value,
                CurrentVerified = req.CurrentVerified
            };
        }
        public async Task VerifyNewEmailCodeAsync(int userId, Guid requestId, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null) throw new KeyNotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (req.NewVerified) return;

            if (req.NewCodeExpiresAt == null || IsExpired(req.NewCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Mã xác thực email mới đã hết hạn.");
            }

            var hashed = HashCode(code);
            if (!hashed.SequenceEqual(req.NewCodeHash ?? Array.Empty<byte>()))
                throw new UnauthorizedAccessException("Mã xác thực không hợp lệ.");

            req.NewVerified = true;
            req.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task CompleteEmailChangeAsync(int userId, Guid requestId)
        {
            var req = await _context.EmailChangeRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null) throw new KeyNotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (!req.CurrentVerified || !req.NewVerified)
                throw new UnauthorizedAccessException("Cần xác nhận cả email hiện tại và email mới trước khi hoàn tất.");

            if (IsExpired(req.CurrentCodeExpiresAt) || IsExpired(req.NewCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedAccessException("Một trong các mã xác thực đã hết hạn.");
            }

            if (string.IsNullOrWhiteSpace(req.NewEmail))
                throw new InvalidOperationException("Email mới chưa được thiết lập trên request.");

            var newEmail = req.NewEmail.Trim().ToLowerInvariant();
            var existing = await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (existing)
                throw new InvalidOperationException("Email mới đã được sử dụng bởi tài khoản khác.");

            var user = req.User!;
            user.Email = newEmail;
            user.EmailConfirmed = true;
            req.IsConsumed = true;
            req.UpdatedAt = DateTime.UtcNow;

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
            foreach (var t in tokens) t.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
