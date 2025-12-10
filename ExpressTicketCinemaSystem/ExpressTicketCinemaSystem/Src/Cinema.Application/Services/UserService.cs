using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System.Collections.Generic;
using System.Linq;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using System; 

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class UserService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private const int CodeExpiryMinutes = 10;

        public UserService(
            CinemaDbCoreContext context,
            IConfiguration config,
            IEmailService emailService,
            IPasswordHasher<User> passwordHasher,
            IAuditLogService auditLogService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        public async Task<UserProfileResponse> GetProfileAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");

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
                Email = user.Email ?? string.Empty,
                Role = user.UserType ?? "User",
                AccountStatus = GetAccountStatus(user),
                IsBanned = user.IsBanned,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt
            };
        }
        private string GetAccountStatus(User user)
        {
            if (user.IsBanned) return "banned";
            if (!user.IsActive) return "inactive";
            if (!user.EmailConfirmed) return "email_not_verified";
            return "active";
        }
        public async Task<UserProfileResponse> UpdateProfileAsync(int userId, UpdateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");
            var errors = new Dictionary<string, ValidationError>();

            if (request.Fullname != null)
            {
                var fullnameTrim = request.Fullname.Trim();
                if (string.IsNullOrEmpty(fullnameTrim))
                    errors["fullname"] = new ValidationError { Msg = "Fullname không được rỗng.", Path = "fullname", Location = "body" };
                else if (fullnameTrim.Length > 255)
                    errors["fullname"] = new ValidationError { Msg = "Fullname quá dài (tối đa 255 ký tự).", Path = "fullname", Location = "body" };
            }

            if (request.Phone != null)
            {
                var phone = request.Phone.Trim();
                if (!Regex.IsMatch(phone, @"^0\d{9}$"))
                    errors["phone"] = new ValidationError { Msg = "Phone phải có đúng 10 chữ số và bắt đầu bằng '0'.", Path = "phone", Location = "body" };
            }

            if (request.AvatarUrl != null && request.AvatarUrl.Length > 2000)
            {
                errors["avatarUrl"] = new ValidationError { Msg = "AvatarUrl quá dài.", Path = "avatarUrl", Location = "body" };
            }

            if (errors.Any())
                throw new ValidationException(errors);

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
            var beforeSnapshot = new { user.UserId, user.Password };

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEntityChangeAsync(
                action: "USER_CHANGE_PASSWORD",
                tableName: "User",
                recordId: user.UserId,
                beforeData: beforeSnapshot,
                afterData: new { user.UserId });
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
                throw new ValidationException("form", "Vui lòng cung cấp mật khẩu cũ, mật khẩu mới và xác nhận mật khẩu.");
            }

            if (request.NewPassword != request.ConfirmPassword)
                throw new ValidationException("confirmPassword", "Mật khẩu mới và xác nhận mật khẩu không khớp.");

            var pwdPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,12}$";
            if (!Regex.IsMatch(request.NewPassword, pwdPattern))
                throw new ValidationException("newPassword", "Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");

            if (!user.IsActive)
                throw new UnauthorizedException("Tài khoản đã bị khóa.");

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new UnauthorizedException("Mật khẩu cũ không chính xác.");

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
            return RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
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
            if (user == null) throw new NotFoundException("Người dùng không tồn tại.");
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ValidationException("account", "Tài khoản không có email để thay đổi.");

            var existing = await _context.EmailChangeRequests
                .Where(r => r.UserId == userId && !r.IsConsumed)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null && !IsExpired(existing.CurrentCodeExpiresAt))
            {
                if (!existing.CurrentVerified)
                {
                    return new RequestEmailChangeResponse
                    {
                        RequestId = existing.RequestId,
                        ExpiresAt = existing.CurrentCodeExpiresAt!.Value,
                        CurrentVerified = existing.CurrentVerified
                    };
                }
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
                throw new ValidationException("code", "Code không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null)
                throw new NotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (req.CurrentCodeHash == null || req.CurrentCodeHash.Length == 0)
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new ValidationException("request", "Không tìm thấy mã hiện tại trên hệ thống — vui lòng tạo lại yêu cầu đổi email.");
            }

            if (IsExpired(req.CurrentCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedException("Mã xác thực hiện tại đã hết hạn.");
            }

            var hashed = HashCode(code);

            if (!hashed.SequenceEqual(req.CurrentCodeHash))
            {
                throw new UnauthorizedException("Mã xác thực không hợp lệ.");
            }

            req.CurrentVerified = true;
            req.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<RequestEmailChangeResponse> SubmitNewEmailAsync(int userId, SubmitNewEmailRequest model)
        {
            if (model == null)
                throw new ValidationException("request", "Request body không được rỗng.");
            if (model.RequestId == Guid.Empty)
                throw new ValidationException("requestId", "Thiếu requestId.");
            if (string.IsNullOrWhiteSpace(model.NewEmail))
                throw new ValidationException("newEmail", "NewEmail không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId && r.UserId == userId && !r.IsConsumed);

            if (req == null)
                throw new NotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (!req.CurrentVerified)
                throw new UnauthorizedException("Chưa xác thực email hiện tại.");

            if (IsExpired(req.CurrentCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedException("Yêu cầu đã hết hạn. Vui lòng tạo lại yêu cầu.");
            }

            var newEmail = model.NewEmail.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ValidationException("newEmail", "Định dạng email mới không hợp lệ.");

            var exists = await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (exists)
                throw new ConflictException("newEmail", "Email mới đã được sử dụng bởi tài khoản khác.");

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
            if (string.IsNullOrWhiteSpace(code))
                throw new ValidationException("code", "Code không được rỗng.");

            var req = await _context.EmailChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null)
                throw new NotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (req.NewVerified) return;

            if (req.NewCodeExpiresAt == null || IsExpired(req.NewCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedException("Mã xác thực email mới đã hết hạn.");
            }

            var hashed = HashCode(code);
            if (!hashed.SequenceEqual(req.NewCodeHash ?? Array.Empty<byte>()))
                throw new UnauthorizedException("Mã xác thực không hợp lệ.");

            req.NewVerified = true;
            req.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task CompleteEmailChangeAsync(int userId, Guid requestId)
        {
            var req = await _context.EmailChangeRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.UserId == userId && !r.IsConsumed);

            if (req == null)
                throw new NotFoundException("Yêu cầu đổi email không tồn tại hoặc đã được sử dụng.");

            if (!req.CurrentVerified || !req.NewVerified)
                throw new UnauthorizedException("Cần xác nhận cả email hiện tại và email mới trước khi hoàn tất.");

            if (IsExpired(req.CurrentCodeExpiresAt) || IsExpired(req.NewCodeExpiresAt))
            {
                req.IsConsumed = true;
                req.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw new UnauthorizedException("Một trong các mã xác thực đã hết hạn.");
            }

            if (string.IsNullOrWhiteSpace(req.NewEmail))
                throw new ValidationException("request", "Email mới chưa được thiết lập trên request.");

            var newEmail = req.NewEmail.Trim().ToLowerInvariant();
            var existing = await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (existing)
                throw new ConflictException("newEmail", "Email mới đã được sử dụng bởi tài khoản khác.");

            var user = req.User!;
            user.Email = newEmail;
            user.EmailConfirmed = true;
            req.IsConsumed = true;
            req.UpdatedAt = DateTime.UtcNow;

            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
            foreach (var t in tokens) t.IsRevoked = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get user's booking orders with filtering and pagination
        /// </summary>
        public async Task<UserOrdersResponse> GetUserOrdersAsync(int userId, GetUserOrdersRequest request)
        {
            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Check if user exists
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");

            // Get customer_id from user_id
            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                // User has no customer record, return empty list
                return new UserOrdersResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new List<UserOrderItemDto>()
                };
            }

            // Build query
            var query = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == customer.CustomerId)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusLower = request.Status.Trim().ToLower();
                query = query.Where(b => 
                    b.Status.ToLower() == statusLower || 
                    b.State.ToLower() == statusLower || 
                    (b.PaymentStatus != null && b.PaymentStatus.ToLower() == statusLower));
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(b => b.BookingTime >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                // Include the entire day
                var toDateEndOfDay = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(b => b.BookingTime < toDateEndOfDay);
            }

            // Get total count
            var totalItems = await query.CountAsync();

            // Apply pagination and ordering
            var bookings = await query
                .OrderByDescending(b => b.BookingTime)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to response DTO
            var items = bookings.Select(b => new UserOrderItemDto
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                BookingTime = b.BookingTime,
                Status = b.Status,
                State = b.State,
                PaymentStatus = b.PaymentStatus,
                TotalAmount = b.TotalAmount,
                TicketCount = b.Tickets.Count,
                Showtime = new OrderShowtimeDto
                {
                    ShowtimeId = b.Showtime.ShowtimeId,
                    ShowDatetime = b.Showtime.ShowDatetime,
                    FormatType = b.Showtime.FormatType
                },
                Movie = new OrderMovieDto
                {
                    MovieId = b.Showtime.Movie.MovieId,
                    Title = b.Showtime.Movie.Title,
                    DurationMinutes = b.Showtime.Movie.DurationMinutes,
                    PosterUrl = b.Showtime.Movie.PosterUrl
                },
                Cinema = new OrderCinemaDto
                {
                    CinemaId = b.Showtime.Cinema.CinemaId,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    Address = b.Showtime.Cinema.Address,
                    City = b.Showtime.Cinema.City,
                    District = b.Showtime.Cinema.District
                }
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

            return new UserOrdersResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }

        /// <summary>
        /// Get detailed information of a specific booking order
        /// </summary>
        public async Task<UserOrderDetailResponse> GetUserOrderDetailAsync(int userId, int bookingId)
        {
            // Check if user exists
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");

            // Get customer_id from user_id
            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            // Query booking with authorization check (must belong to this customer)
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(b => b.ServiceOrders)
                    .ThenInclude(so => so.Service)
                .Include(b => b.Voucher)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customer.CustomerId);

            // If booking not found or doesn't belong to this user
            if (booking == null)
                throw new NotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.");

            // Get SeatTickets for check-in status
            var seatTickets = await _context.SeatTickets
                .Where(st => st.BookingId == bookingId)
                .ToDictionaryAsync(st => st.TicketId, st => st);

            // Calculate totals from Tickets and ServiceOrders
            var ticketsTotal = booking.Tickets.Sum(t => t.Price);
            var combosTotal = booking.ServiceOrders.Sum(so => so.UnitPrice * so.Quantity);

            // Map to response DTO
            var response = new UserOrderDetailResponse
            {
                Booking = new OrderDetailBookingDto
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    BookingTime = booking.BookingTime,
                    TotalAmount = booking.TotalAmount,
                    TicketsTotal = ticketsTotal,
                    CombosTotal = combosTotal,
                    Status = booking.Status,
                    State = booking.State,
                    PaymentStatus = booking.PaymentStatus,
                    PaymentProvider = booking.PaymentProvider,
                    PaymentTxId = booking.PaymentTxId,
                    VoucherId = booking.VoucherId,
                    OrderCode = booking.OrderCode,
                    SessionId = booking.SessionId,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                },
                Showtime = new OrderDetailShowtimeDto
                {
                    ShowtimeId = booking.Showtime.ShowtimeId,
                    ShowDatetime = booking.Showtime.ShowDatetime,
                    EndTime = booking.Showtime.EndTime,
                    Status = booking.Showtime.Status,
                    BasePrice = booking.Showtime.BasePrice,
                    FormatType = booking.Showtime.FormatType
                },
                Movie = new OrderDetailMovieDto
                {
                    MovieId = booking.Showtime.Movie.MovieId,
                    Title = booking.Showtime.Movie.Title,
                    Genre = booking.Showtime.Movie.Genre,
                    DurationMinutes = booking.Showtime.Movie.DurationMinutes,
                    Language = booking.Showtime.Movie.Language,
                    Director = booking.Showtime.Movie.Director,
                    Country = booking.Showtime.Movie.Country,
                    PosterUrl = booking.Showtime.Movie.PosterUrl,
                    BannerUrl = booking.Showtime.Movie.BannerUrl,
                    Description = booking.Showtime.Movie.Description
                },
                Cinema = new OrderDetailCinemaDto
                {
                    CinemaId = booking.Showtime.Cinema.CinemaId,
                    CinemaName = booking.Showtime.Cinema.CinemaName,
                    Address = booking.Showtime.Cinema.Address,
                    City = booking.Showtime.Cinema.City,
                    District = booking.Showtime.Cinema.District,
                    Phone = booking.Showtime.Cinema.Phone,
                    Email = booking.Showtime.Cinema.Email
                },
                Tickets = booking.Tickets.Select(t => 
                {
                    var seatTicket = seatTickets.GetValueOrDefault(t.TicketId);
                    return new OrderDetailTicketDto
                    {
                        TicketId = t.TicketId,
                        Price = t.Price,
                        Status = t.Status,
                        CheckInStatus = seatTicket?.CheckInStatus ?? "NOT_CHECKED_IN",
                        CheckInTime = seatTicket?.CheckInTime,
                        Seat = new OrderDetailSeatDto
                        {
                            SeatId = t.Seat.SeatId,
                            RowCode = t.Seat.RowCode,
                            SeatNumber = t.Seat.SeatNumber,
                            SeatName = t.Seat.SeatName ?? $"{t.Seat.RowCode}{t.Seat.SeatNumber}",
                            SeatTypeName = t.Seat.SeatType?.Name
                        }
                    };
                }).ToList(),
                Combos = booking.ServiceOrders.Select(so => new OrderDetailComboDto
                {
                    ServiceId = so.ServiceId,
                    ServiceName = so.Service?.ServiceName ?? "Unknown",
                    Quantity = so.Quantity,
                    UnitPrice = so.UnitPrice,
                    SubTotal = so.UnitPrice * so.Quantity
                }).ToList(),
                Voucher = booking.Voucher != null ? new OrderDetailVoucherDto
                {
                    VoucherId = booking.Voucher.VoucherId,
                    VoucherCode = booking.Voucher.VoucherCode,
                    DiscountType = booking.Voucher.DiscountType,
                    DiscountVal = booking.Voucher.DiscountVal
                } : null
            };

            return response;
        }

        /// <summary>
        /// Get user's tickets list with filtering and pagination (grouped by individual tickets)
        /// </summary>
        public async Task<UserTicketsResponse> GetUserTicketsAsync(int userId, GetUserTicketsRequest request)
        {
            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Validate type parameter
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var validTypes = new[] { "upcoming", "past", "all" };
                var typeLower = request.Type.Trim().ToLower();
                if (!validTypes.Contains(typeLower))
                    throw new ValidationException("type", "Type phải là một trong: upcoming, past, all.");
            }

            // Check if user exists
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("Người dùng không tồn tại.");

            // Get customer_id from user_id
            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                // User has no customer record, return empty list
                return new UserTicketsResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new List<UserTicketItemDto>()
                };
            }

            // Build query: Ticket → Booking → Showtime → Movie, Cinema + Seat
            var query = _context.Tickets
                .Include(t => t.Booking)
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(t => t.Seat)
                    .ThenInclude(s => s.SeatType)
                .Where(t => t.Booking.CustomerId == customer.CustomerId)
                .AsNoTracking();

            // Apply type filter
            var now = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var typeLower = request.Type.Trim().ToLower();
                switch (typeLower)
                {
                    case "upcoming":
                        // Sắp chiếu: show_datetime >= now và ticket status = ACTIVE
                        query = query.Where(t => t.Showtime.ShowDatetime >= now && t.Status == "ACTIVE");
                        break;
                    case "past":
                        // Đã chiếu: show_datetime < now
                        query = query.Where(t => t.Showtime.ShowDatetime < now);
                        break;
                    case "all":
                        // Không filter gì cả
                        break;
                }
            }

            // Get total count
            var totalItems = await query.CountAsync();

            // Apply ordering and pagination
            var tickets = await query
                .OrderByDescending(t => t.Showtime.ShowDatetime) // Sắp xếp theo thời gian chiếu
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Get SeatTickets for check-in status
            var ticketIds = tickets.Select(t => t.TicketId).ToList();
            var seatTicketsDict = await _context.SeatTickets
                .Where(st => ticketIds.Contains(st.TicketId))
                .ToDictionaryAsync(st => st.TicketId, st => st);

            // Map to response DTO
            var items = tickets.Select(t => 
            {
                var seatTicket = seatTicketsDict.GetValueOrDefault(t.TicketId);
                var seatName = t.Seat.SeatName ?? $"{t.Seat.RowCode}{t.Seat.SeatNumber}";
                var ticketQR = $"{seatName}{t.Booking.BookingCode}";
                
                return new UserTicketItemDto
                {
                    TicketId = t.TicketId,
                    Price = t.Price,
                    Status = t.Status,
                    CheckInStatus = seatTicket?.CheckInStatus ?? "NOT_CHECKED_IN",
                    CheckInTime = seatTicket?.CheckInTime,
                    TicketQR = ticketQR,
                    Booking = new TicketBookingDto
                    {
                        BookingId = t.Booking.BookingId,
                        BookingCode = t.Booking.BookingCode,
                        PaymentStatus = t.Booking.PaymentStatus
                    },
                    Movie = new TicketMovieDto
                    {
                        MovieId = t.Showtime.Movie.MovieId,
                        Title = t.Showtime.Movie.Title,
                        DurationMinutes = t.Showtime.Movie.DurationMinutes,
                        PosterUrl = t.Showtime.Movie.PosterUrl,
                        Genre = t.Showtime.Movie.Genre
                    },
                    Cinema = new TicketCinemaDto
                    {
                        CinemaId = t.Showtime.Cinema.CinemaId,
                        CinemaName = t.Showtime.Cinema.CinemaName,
                        Address = t.Showtime.Cinema.Address,
                        City = t.Showtime.Cinema.City,
                        District = t.Showtime.Cinema.District
                    },
                    Showtime = new TicketShowtimeDto
                    {
                        ShowtimeId = t.Showtime.ShowtimeId,
                        ShowDatetime = t.Showtime.ShowDatetime,
                        EndTime = t.Showtime.EndTime,
                        FormatType = t.Showtime.FormatType,
                        Status = t.Showtime.Status
                    },
                    Seat = new TicketSeatDto
                    {
                        SeatId = t.Seat.SeatId,
                        RowCode = t.Seat.RowCode,
                        SeatNumber = t.Seat.SeatNumber,
                        SeatName = seatName,
                        SeatTypeName = t.Seat.SeatType?.Name
                    }
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

            return new UserTicketsResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }
    }
}