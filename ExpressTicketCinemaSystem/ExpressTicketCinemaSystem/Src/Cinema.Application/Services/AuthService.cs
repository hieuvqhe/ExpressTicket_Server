using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
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
        private readonly IAuditLogService _auditLogService;

        public AuthService(
            CinemaDbCoreContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService,
            IConfiguration config,
            IAuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _config = config;
            _auditLogService = auditLogService;
        }

        public async Task<User> RegisterAsync(RegisterRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new ValidationError { Msg = "Họ và tên là bắt buộc", Path = "fullName" };

            if (string.IsNullOrWhiteSpace(request.Username))
                errors["username"] = new ValidationError { Msg = "Tên đăng nhập là bắt buộc", Path = "username" };

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["email"] = new ValidationError { Msg = "Email là bắt buộc", Path = "email" };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new ValidationError { Msg = "Mật khẩu là bắt buộc", Path = "password" };

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                errors["confirmPassword"] = new ValidationError { Msg = "Xác nhận mật khẩu là bắt buộc", Path = "confirmPassword" };

            // Validate password match
            if (request.Password != request.ConfirmPassword)
                errors["confirmPassword"] = new ValidationError { Msg = "Mật khẩu và xác nhận mật khẩu không khớp", Path = "confirmPassword" };

            // Validate username format
            if (!string.IsNullOrWhiteSpace(request.Username) && !Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]{8,15}$"))
                errors["username"] = new ValidationError { Msg = "Tên đăng nhập phải từ 8-15 ký tự và chỉ chứa chữ cái, số và dấu gạch dưới", Path = "username" };

            // Validate email format
            if (!string.IsNullOrWhiteSpace(request.Email) && !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = new ValidationError { Msg = "Định dạng email không hợp lệ", Path = "email" };

            // Validate password strength
            if (!string.IsNullOrWhiteSpace(request.Password) && !Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,12}$"))
                errors["password"] = new ValidationError { Msg = "Mật khẩu phải từ 6-12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt", Path = "password" };

            if (errors.Any())
                throw new ValidationException(errors);

            // Check for existing users
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                throw new ConflictException("username", "Tên đăng nhập đã tồn tại");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new ConflictException("email", "Email đã tồn tại");

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

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_RESET_PASSWORD",
                tableName: "User",
                recordId: user.UserId,
                beforeData: null,
                afterData: new { user.UserId });

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

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(token))
            {
                errors["token"] = new ValidationError { Msg = "Token là bắt buộc", Path = "token" };
                throw new ValidationException(errors);
            }

            var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            var verification = await _context.EmailVerificationTokens
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.TokenHash == tokenBytes);

            if (verification == null)
            {
                errors["token"] = new ValidationError { Msg = "Token không hợp lệ", Path = "token" };
                throw new ValidationException(errors);
            }

            if (verification.ExpiresAt < DateTime.UtcNow)
            {
                errors["token"] = new ValidationError { Msg = "Token đã hết hạn", Path = "token" };
                throw new ValidationException(errors);
            }

            verification.User.EmailConfirmed = true;
            verification.ConsumedAt = DateTime.UtcNow;

            _context.EmailVerificationTokens.Remove(verification);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_VERIFY_EMAIL",
                tableName: "User",
                recordId: verification.UserId,
                beforeData: null,
                afterData: new
                {
                    verification.UserId,
                    verification.User.Email,
                    EmailConfirmed = true
                });
            return true;
        }

        public async Task ResendVerificationEmailAsync(string email)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors["email"] = new ValidationError { Msg = "Email là bắt buộc", Path = "email" };
                throw new ValidationException(errors);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                errors["email"] = new ValidationError { Msg = "Không tìm thấy người dùng với email này", Path = "email" };
                throw new ValidationException(errors);
            }

            if (user.EmailConfirmed)
                throw new ConflictException("email", "Email đã được xác minh");

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

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_RESEND_VERIFICATION",
                tableName: "User",
                recordId: user.UserId,
                beforeData: null,
                afterData: new
                {
                    user.UserId,
                    user.Email
                });
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
                errors["emailOrUsername"] = new ValidationError { Msg = "Email/Tên đăng nhập không được để trống", Path = "emailOrUsername" };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new ValidationError { Msg = "Mật khẩu không được để trống", Path = "password" };

            if (errors.Any())
                throw new ValidationException(errors);

            // Tìm user - INCLUDE PARTNER AND EMPLOYEE INFORMATION
            var user = await _context.Users
                .Include(u => u.Partner) // QUAN TRỌNG: Include partner info
                .Include(u => u.Employee) // QUAN TRỌNG: Include employee info for Staff validation
                    .ThenInclude(e => e.Partner) // Include Partner info from Employee
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["emailOrUsername"] = new ValidationError
                    {
                        Msg = "Tài khoản hoặc mật khẩu không chính xác",
                        Path = "emailOrUsername"
                    }
                });

            // ==================== PARTNER SPECIFIC VALIDATION ====================
            if (user.UserType == "Partner")
            {
                ValidatePartnerLogin(user);
            }

            // ==================== STAFF/EMPLOYEE SPECIFIC VALIDATION ====================
            if (user.UserType == "Staff" || user.UserType == "Marketing" || user.UserType == "Cashier")
            {
                await ValidateStaffLoginAsync(user);
            }

            // ==================== COMMON VALIDATION ====================
            if (!user.EmailConfirmed)
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["email"] = new ValidationError
                    {
                        Msg = "Email chưa được xác minh. Vui lòng kiểm tra email",
                        Path = "email"
                    }
                });
            if (user.IsBanned)
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["account"] = new ValidationError
                    {
                        Msg = "Tài khoản đang bị cấm. Vui lòng liên hệ admin để mở khóa",
                        Path = "account"
                    }
                });
            if (!user.IsActive)
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["account"] = new ValidationError
                    {
                        Msg = "Tài khoản đã bị khóa",
                        Path = "account"
                    }
                });

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["password"] = new ValidationError
                    {
                        Msg = "Tài khoản hoặc mật khẩu không chính xác",
                        Path = "password"
                    }
                });

            var response = await CreateJwtResponseAsync(user);

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_LOGIN_SUCCESS",
                tableName: "User",
                recordId: user.UserId,
                beforeData: null,
                afterData: new
                {
                    user.UserId,
                    user.Email,
                    user.Username,
                    user.UserType,
                    response.ExpireAt
                });

            return response;
        }

        // ==================== NEW METHOD: VALIDATE PARTNER LOGIN ====================
        private void ValidatePartnerLogin(User user)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Kiểm tra partner record có tồn tại không
            if (user.Partner == null)
            {
                errors["partner"] = new ValidationError
                {
                    Msg = "Tài khoản partner chưa được thiết lập đầy đủ. Vui lòng liên hệ quản trị viên.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }

            // Kiểm tra partner status
            switch (user.Partner.Status?.ToLower())
            {
                case "pending":
                    errors["status"] = new ValidationError
                    {
                        Msg = "Tài khoản partner đang chờ duyệt. Vui lòng chờ quản trị viên xét duyệt.",
                        Path = "account"
                    };
                    break;

                case "rejected":
                    var reason = string.IsNullOrEmpty(user.Partner.RejectionReason)
                        ? "Lý do: Không được cung cấp"
                        : $"Lý do: {user.Partner.RejectionReason}";

                    errors["status"] = new ValidationError
                    {
                        Msg = $"Tài khoản partner đã bị từ chối. {reason}",
                        Path = "account"
                    };
                    break;

                case "approved":
                    // Partner approved - cho phép login
                    break;

                case null:
                case "":
                    errors["status"] = new ValidationError
                    {
                        Msg = "Trạng thái partner không hợp lệ. Vui lòng liên hệ quản trị viên.",
                        Path = "account"
                    };
                    break;

                default:
                    errors["status"] = new ValidationError
                    {
                        Msg = $"Trạng thái partner không xác định: {user.Partner.Status}",
                        Path = "account"
                    };
                    break;
            }

            if (errors.Any())
                throw new UnauthorizedException(errors);
        }

        // ==================== NEW METHOD: VALIDATE STAFF/EMPLOYEE LOGIN ====================
        private async Task ValidateStaffLoginAsync(User user)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Kiểm tra employee record có tồn tại không
            if (user.Employee == null)
            {
                errors["employee"] = new ValidationError
                {
                    Msg = "Tài khoản nhân viên chưa được thiết lập đầy đủ. Vui lòng liên hệ quản trị viên.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }

            // Kiểm tra employee is active
            if (!user.Employee.IsActive)
            {
                errors["employee"] = new ValidationError
                {
                    Msg = "Tài khoản nhân viên đã bị khóa. Vui lòng liên hệ quản lý để được kích hoạt lại.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }

            // Kiểm tra partner của employee có active không (vì Staff phụ thuộc vào Partner)
            if (user.Employee.Partner == null)
            {
                errors["partner"] = new ValidationError
                {
                    Msg = "Thông tin đối tác chưa được thiết lập. Vui lòng liên hệ quản trị viên.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }

            if (!user.Employee.Partner.IsActive)
            {
                errors["partner"] = new ValidationError
                {
                    Msg = "Tài khoản đối tác của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }

            // Kiểm tra partner status
            if (user.Employee.Partner.Status?.ToLower() != "approved")
            {
                errors["partner"] = new ValidationError
                {
                    Msg = "Tài khoản đối tác chưa được duyệt. Vui lòng liên hệ quản trị viên.",
                    Path = "account"
                };
                throw new UnauthorizedException(errors);
            }
        }

        public async Task<LoginResponse> CreateJwtResponseAsync(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("Lỗi cấu hình server: Jwt:Key không được thiết lập");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            // Tạo claims
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Fullname ?? ""),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, user.UserType ?? "User")
    };

            // ==================== ADD PARTNER SPECIFIC CLAIMS ====================
            if (user.UserType == "Partner" && user.Partner != null)
            {
                claims.Add(new Claim("PartnerId", user.Partner.PartnerId.ToString()));
                claims.Add(new Claim("PartnerStatus", user.Partner.Status ?? ""));
                claims.Add(new Claim("PartnerName", user.Partner.PartnerName ?? ""));

                if (user.Partner.CommissionRate > 0)
                {
                    claims.Add(new Claim("CommissionRate", user.Partner.CommissionRate.ToString()));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

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

            // ==================== ENHANCE LOGIN RESPONSE FOR PARTNER ====================
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refresh.Token,
                ExpireAt = tokenDescriptor.Expires!.Value,
                FullName = user.Fullname ?? "",
                Role = user.UserType ?? "User",
                PartnerInfo = null,
                // CHỈ CẦN THÊM THÔNG TIN TRẠNG THÁI VÀO RESPONSE
                AccountStatus = GetAccountStatus(user),
                IsBanned = user.IsBanned,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed
            };

            // Thêm thông tin partner vào response nếu là partner
            if (user.UserType == "Partner" && user.Partner != null)
            {
                response.PartnerInfo = new PartnerLoginInfo
                {
                    PartnerId = user.Partner.PartnerId,
                    PartnerName = user.Partner.PartnerName,
                    PartnerStatus = user.Partner.Status ?? ""
                };
            }

            return response;
        }
        private string GetAccountStatus(User user)
        {
            if (user.IsBanned) return "banned";
            if (!user.IsActive) return "inactive";
            if (!user.EmailConfirmed) return "email_not_verified";
            return "active";
        }
        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                errors["refreshToken"] = new ValidationError { Msg = "Refresh Token không hợp lệ", Path = "refreshToken" };
                throw new UnauthorizedException(errors);
            }

            var token = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

            if (token == null)
            {
                errors["refreshToken"] = new ValidationError { Msg = "Refresh Token không hợp lệ", Path = "refreshToken" };
                throw new UnauthorizedException(errors);
            }

            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                errors["refreshToken"] = new ValidationError { Msg = "Refresh Token đã hết hạn", Path = "refreshToken" };
                throw new UnauthorizedException(errors);
            }

            if (token.User == null || !token.User.IsActive)
            {
                errors["account"] = new ValidationError { Msg = "Tài khoản liên kết với token này đã bị khóa", Path = "account" };
                throw new UnauthorizedException(errors);
            }

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
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                errors["refreshToken"] = new ValidationError { Msg = "Refresh token không hợp lệ", Path = "refreshToken" };
                throw new ValidationException(errors);
            }

            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token == null)
            {
                errors["refreshToken"] = new ValidationError { Msg = "Refresh token không hợp lệ", Path = "refreshToken" };
                throw new ValidationException(errors);
            }

            token.IsRevoked = true;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_LOGOUT",
                tableName: "User",
                recordId: token.UserId,
                beforeData: null,
                afterData: new { token.UserId, refreshToken });
            return true;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Email hoặc tên đăng nhập là bắt buộc", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Không tìm thấy tài khoản với email hoặc username này", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            if (!user.IsActive)
                throw new ConflictException("emailOrUsername", "Tài khoản đã bị khóa, không thể khôi phục mật khẩu");

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

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_FORGOT_PASSWORD",
                tableName: "User",
                recordId: user.UserId,
                beforeData: null,
                afterData: new { user.UserId, user.Email });
        }

        public async Task VerifyResetCodeAsync(VerifyResetCodeRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Email hoặc tên đăng nhập là bắt buộc", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors["code"] = new ValidationError { Msg = "Mã xác minh là bắt buộc", Path = "code" };
                throw new ValidationException(errors);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Không tìm thấy người dùng", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            var record = await _context.PasswordResetCodes
                .Where(c => c.UserId == user.UserId && c.Code == request.Code && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                errors["code"] = new ValidationError { Msg = "Mã không hợp lệ", Path = "code" };
                throw new ValidationException(errors);
            }

            if (record.ExpiredAt < DateTime.UtcNow)
            {
                errors["code"] = new ValidationError { Msg = "Mã xác minh đã hết hạn, vui lòng yêu cầu mã mới", Path = "code" };
                throw new ValidationException(errors);
            }

            record.IsVerified = true;
            record.VerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEntityChangeAsync(
                action: "AUTH_VERIFY_RESET_CODE",
                tableName: "User",
                recordId: user.UserId,
                beforeData: null,
                afterData: new { user.UserId, request.Code });
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Email hoặc tên đăng nhập là bắt buộc", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                errors["newPassword"] = new ValidationError { Msg = "Mật khẩu mới là bắt buộc", Path = "newPassword" };
                throw new ValidationException(errors);
            }

            if (string.IsNullOrWhiteSpace(request.VerifyPassword))
            {
                errors["verifyPassword"] = new ValidationError { Msg = "Xác nhận mật khẩu là bắt buộc", Path = "verifyPassword" };
                throw new ValidationException(errors);
            }

            if (request.NewPassword != request.VerifyPassword)
            {
                errors["verifyPassword"] = new ValidationError { Msg = "Hai mật khẩu không khớp", Path = "verifyPassword" };
                throw new ValidationException(errors);
            }

            if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,12}$"))
            {
                errors["newPassword"] = new ValidationError { Msg = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự)", Path = "newPassword" };
                throw new ValidationException(errors);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            if (user == null)
            {
                errors["emailOrUsername"] = new ValidationError { Msg = "Không tìm thấy người dùng", Path = "emailOrUsername" };
                throw new ValidationException(errors);
            }

            var verified = await _context.PasswordResetCodes
                .Where(c => c.UserId == user.UserId && c.IsVerified && !c.IsUsed && c.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(c => c.VerifiedAt)
                .FirstOrDefaultAsync();

            if (verified == null)
            {
                errors["code"] = new ValidationError { Msg = "Mã xác minh không hợp lệ hoặc chưa được xác minh", Path = "code" };
                throw new ValidationException(errors);
            }

            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
            verified.IsUsed = true;

            await _context.SaveChangesAsync();
        }
    }
}