using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IVoucherService
    {
        Task<VoucherResponse> CreateVoucherAsync(int managerId, CreateVoucherRequest request);
        Task<PaginatedVouchersResponse> GetAllVouchersAsync(int managerId, int page, int limit, string? search, string? status, string? sortBy, string? sortOrder);
        Task<VoucherResponse> GetVoucherByIdAsync(int voucherId, int managerId);
        Task<VoucherResponse> UpdateVoucherAsync(int voucherId, int managerId, UpdateVoucherRequest request);
        Task SoftDeleteVoucherAsync(int voucherId, int managerId);
        Task ToggleVoucherStatusAsync(int voucherId, int managerId);
        Task<List<VoucherEmailHistoryResponse>> GetVoucherEmailHistoryAsync(int voucherId, int managerId);
        Task<bool> ValidateVoucherCodeAsync(string voucherCode, int? excludeVoucherId = null);

        // THÊM MỚI - Gửi voucher cho tất cả users
        Task<SendVoucherEmailResponse> SendVoucherToAllUsersAsync(int voucherId, int managerId, SendVoucherToAllRequest request);

        // THÊM MỚI - Gửi voucher cho users cụ thể
        Task<SendVoucherEmailResponse> SendVoucherToSpecificUsersAsync(int voucherId, int managerId, SendVoucherEmailRequest request);

        Task<List<UserVoucherResponse>> GetValidVouchersForUserAsync();

        // THÊM MỚI - Validate voucher khi user sử dụng
        Task<VoucherValidationResponse> ValidateVoucherForUserAsync(string voucherCode, decimal totalAmount);

        // THÊM MỚI - Lấy voucher theo code
        Task<UserVoucherResponse?> GetVoucherByCodeAsync(string voucherCode);

        Task<UserVoucherResponse?> GetVoucherByIdForUserAsync(int voucherId);
    }

    public class VoucherService : IVoucherService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IManagerService _managerService;
        private readonly IEmailService _emailService;
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(CinemaDbCoreContext context, IManagerService managerService, IEmailService emailService, ILogger<VoucherService> logger)
        {
            _context = context;
            _managerService = managerService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<VoucherResponse> CreateVoucherAsync(int managerId, CreateVoucherRequest request)
        {
            // Validate manager exists
            if (!await _managerService.ValidateManagerExistsAsync(managerId))
            {
                throw new NotFoundException("Manager không tồn tại");
            }

            // Validate voucher code uniqueness
            if (await ValidateVoucherCodeAsync(request.VoucherCode))
            {
                throw new ValidationException("VoucherCode", "Mã voucher đã tồn tại");
            }

            // Validate dates
            if (request.ValidTo <= request.ValidFrom)
            {
                throw new ValidationException("ValidTo", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            // NEW: Validate ValidFrom không được ở quá khứ
            if (request.ValidFrom < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new ValidationException("ValidFrom", "Ngày bắt đầu không được ở quá khứ");
            }

            // Validate discount value for percent type
            if (request.DiscountType == "percent" && request.DiscountVal > 100)
            {
                throw new ValidationException("DiscountVal", "Giá trị giảm giá phần trăm không được vượt quá 100");
            }

            var voucher = new Voucher
            {
                VoucherCode = request.VoucherCode.ToUpper(),
                DiscountType = request.DiscountType,
                DiscountVal = request.DiscountVal,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                UsageLimit = request.UsageLimit,
                Description = request.Description,
                IsActive = request.IsActive,
                ManagerId = managerId,
                CreatedAt = DateTime.UtcNow,
                UsedCount = 0,
                IsDeleted = false
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return await MapToVoucherResponseAsync(voucher);
        }

        public async Task<PaginatedVouchersResponse> GetAllVouchersAsync(int managerId, int page, int limit, string? search, string? status, string? sortBy, string? sortOrder)
        {
            var query = _context.Vouchers
                .Include(v => v.Manager)
                .ThenInclude(m => m.User)
                .Where(v => v.ManagerId == managerId && !v.IsDeleted);

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.VoucherCode.Contains(search) ||
                                        (v.Description != null && v.Description.Contains(search)));
            }

            // Status filter - FIXED
            if (!string.IsNullOrEmpty(status))
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(v => v.IsActive &&
                                               v.ValidFrom <= today &&
                                               v.ValidTo >= today);
                        break;
                    case "inactive":
                        query = query.Where(v => !v.IsActive);
                        break;
                    case "expired":
                        query = query.Where(v => v.ValidTo < today);
                        break;
                    case "upcoming":
                        query = query.Where(v => v.ValidFrom > today);
                        break;
                }
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "vouchercode" => sortOrder == "asc" ? query.OrderBy(v => v.VoucherCode) : query.OrderByDescending(v => v.VoucherCode),
                "discountval" => sortOrder == "asc" ? query.OrderBy(v => v.DiscountVal) : query.OrderByDescending(v => v.DiscountVal),
                "validfrom" => sortOrder == "asc" ? query.OrderBy(v => v.ValidFrom) : query.OrderByDescending(v => v.ValidFrom),
                "validto" => sortOrder == "asc" ? query.OrderBy(v => v.ValidTo) : query.OrderByDescending(v => v.ValidTo),
                "usedcount" => sortOrder == "asc" ? query.OrderBy(v => v.UsedCount) : query.OrderByDescending(v => v.UsedCount),
                _ => sortOrder == "asc" ? query.OrderBy(v => v.CreatedAt) : query.OrderByDescending(v => v.CreatedAt)
            };

            // Pagination
            var totalCount = await query.CountAsync();
            var vouchers = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(v => new VoucherListResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    DiscountType = v.DiscountType,
                    DiscountVal = v.DiscountVal,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    IsActive = v.IsActive,
                    CreatedAt = v.CreatedAt,
                    ManagerName = v.Manager.FullName
                })
                .ToListAsync();

            return new PaginatedVouchersResponse
            {
                Vouchers = vouchers,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = limit,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
                }
            };
        }

        public async Task<VoucherResponse> GetVoucherByIdAsync(int voucherId, int managerId)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Manager)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền truy cập voucher này");
            }

            return await MapToVoucherResponseAsync(voucher);
        }

        public async Task<VoucherResponse> UpdateVoucherAsync(int voucherId, int managerId, UpdateVoucherRequest request)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Manager)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền cập nhật voucher này");
            }

            // Validate voucher code uniqueness if changed
            if (!string.IsNullOrEmpty(request.VoucherCode) && request.VoucherCode != voucher.VoucherCode)
            {
                if (await ValidateVoucherCodeAsync(request.VoucherCode, voucherId))
                {
                    throw new ValidationException("VoucherCode", "Mã voucher đã tồn tại");
                }
                voucher.VoucherCode = request.VoucherCode.ToUpper();
            }

            // NEW: Validate ValidFrom không được ở quá khứ nếu có thay đổi
            if (request.ValidFrom.HasValue && request.ValidFrom.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new ValidationException("ValidFrom", "Ngày bắt đầu không được ở quá khứ");
            }

            // Validate dates nếu có thay đổi
            var validFrom = request.ValidFrom ?? voucher.ValidFrom;
            var validTo = request.ValidTo ?? voucher.ValidTo;
            if (validTo <= validFrom)
            {
                throw new ValidationException("ValidTo", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.DiscountType))
                voucher.DiscountType = request.DiscountType;

            if (request.DiscountVal.HasValue)
            {
                // Validate discount value for percent type
                if ((request.DiscountType ?? voucher.DiscountType) == "percent" && request.DiscountVal > 100)
                {
                    throw new ValidationException("DiscountVal", "Giá trị giảm giá phần trăm không được vượt quá 100");
                }
                voucher.DiscountVal = request.DiscountVal.Value;
            }

            if (request.ValidFrom.HasValue)
                voucher.ValidFrom = request.ValidFrom.Value;

            if (request.ValidTo.HasValue)
                voucher.ValidTo = request.ValidTo.Value;

            if (request.UsageLimit.HasValue)
                voucher.UsageLimit = request.UsageLimit.Value;

            if (request.Description != null)
                voucher.Description = request.Description;

            if (request.IsActive.HasValue)
                voucher.IsActive = request.IsActive.Value;

            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToVoucherResponseAsync(voucher);
        }

        public async Task SoftDeleteVoucherAsync(int voucherId, int managerId)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền xóa voucher này");
            }

            voucher.IsDeleted = true;
            voucher.DeletedAt = DateTime.UtcNow;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleVoucherStatusAsync(int voucherId, int managerId)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền thay đổi trạng thái voucher này");
            }

            voucher.IsActive = !voucher.IsActive;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<VoucherEmailHistoryResponse>> GetVoucherEmailHistoryAsync(int voucherId, int managerId)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền xem lịch sử email của voucher này");
            }

            var history = await _context.VoucherEmailHistories
                .Include(h => h.User)
                .Where(h => h.VoucherId == voucherId)
                .OrderByDescending(h => h.SentAt)
                .Select(h => new VoucherEmailHistoryResponse
                {
                    Id = h.Id,
                    UserEmail = h.User.Email,
                    UserName = h.User.Fullname ?? h.User.Username,
                    SentAt = h.SentAt,
                    Status = h.Status
                })
                .ToListAsync();

            return history;
        }

        public async Task<bool> ValidateVoucherCodeAsync(string voucherCode, int? excludeVoucherId = null)
        {
            var query = _context.Vouchers
                .Where(v => v.VoucherCode == voucherCode.ToUpper() && !v.IsDeleted);

            if (excludeVoucherId.HasValue)
            {
                query = query.Where(v => v.VoucherId != excludeVoucherId.Value);
            }

            return await query.AnyAsync();
        }

        private async Task<VoucherResponse> MapToVoucherResponseAsync(Voucher voucher)
        {
            return new VoucherResponse
            {
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                DiscountType = voucher.DiscountType,
                DiscountVal = voucher.DiscountVal,
                ValidFrom = voucher.ValidFrom,
                ValidTo = voucher.ValidTo,
                UsageLimit = voucher.UsageLimit,
                UsedCount = voucher.UsedCount,
                Description = voucher.Description,
                IsActive = voucher.IsActive,
                CreatedAt = voucher.CreatedAt,
                UpdatedAt = voucher.UpdatedAt,
                ManagerId = voucher.ManagerId,
                ManagerName = voucher.Manager.FullName
            };
        }

        // THÊM MỚI - Gửi voucher cho tất cả users
        public async Task<SendVoucherEmailResponse> SendVoucherToAllUsersAsync(int voucherId, int managerId, SendVoucherEmailRequest request)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền gửi voucher này");
            }

            // Lấy tất cả user có user_type = "User" và is_active = true
            var users = await _context.Users
                .Where(u => u.UserType == "User" && u.IsActive && u.EmailConfirmed)
                .ToListAsync();

            return await SendVoucherEmailsAsync(voucher, users, request);
        }

        // THÊM MỚI - Gửi voucher cho users cụ thể
        public async Task<SendVoucherEmailResponse> SendVoucherToSpecificUsersAsync(int voucherId, int managerId, SendVoucherEmailRequest request)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền gửi voucher này");
            }

            if (request.UserIds == null || !request.UserIds.Any())
            {
                throw new ValidationException("UserIds", "Danh sách user không được để trống");
            }

            // Lấy users theo danh sách IDs
            var users = await _context.Users
                .Where(u => request.UserIds.Contains(u.UserId) && u.IsActive && u.EmailConfirmed)
                .ToListAsync();

            return await SendVoucherEmailsAsync(voucher, users, request);
        }

        // Phương thức helper để gửi email
        private async Task<SendVoucherEmailResponse> SendVoucherEmailsAsync(Voucher voucher, List<User> users, SendVoucherEmailRequest request)
        {
            var results = new List<EmailSendResult>();
            var sentCount = 0;
            var failedCount = 0;

            foreach (var user in users)
            {
                try
                {
                    var userName = user.Fullname ?? user.Username;

                    await _emailService.SendVoucherEmailAsync(
                        user.Email,
                        userName,
                        voucher.VoucherCode,
                        voucher.DiscountType,
                        voucher.DiscountVal,
                        voucher.ValidFrom,
                        voucher.ValidTo,
                        request.Subject,
                        request.CustomMessage
                    );

                    // Ghi lịch sử email
                    var emailHistory = new VoucherEmailHistory
                    {
                        VoucherId = voucher.VoucherId,
                        UserId = user.UserId,
                        SentAt = DateTime.UtcNow,
                        Status = "success"
                    };
                    _context.VoucherEmailHistories.Add(emailHistory);

                    results.Add(new EmailSendResult
                    {
                        UserEmail = user.Email,
                        UserName = userName,
                        Success = true,
                        SentAt = DateTime.UtcNow
                    });
                    sentCount++;

                    _logger.LogInformation("Đã gửi voucher {VoucherCode} đến {Email}", voucher.VoucherCode, user.Email);
                }
                catch (Exception ex)
                {
                    // Ghi lịch sử email thất bại
                    var emailHistory = new VoucherEmailHistory
                    {
                        VoucherId = voucher.VoucherId,
                        UserId = user.UserId,
                        SentAt = DateTime.UtcNow,
                        Status = "failed"
                    };
                    _context.VoucherEmailHistories.Add(emailHistory);

                    results.Add(new EmailSendResult
                    {
                        UserEmail = user.Email,
                        UserName = user.Fullname ?? user.Username,
                        Success = false,
                        ErrorMessage = ex.Message,
                        SentAt = DateTime.UtcNow
                    });
                    failedCount++;

                    _logger.LogError(ex, "Lỗi khi gửi voucher {VoucherCode} đến {Email}", voucher.VoucherCode, user.Email);
                }

                // Delay nhẹ để tránh bị rate limit
                await Task.Delay(100);
            }

            await _context.SaveChangesAsync();

            return new SendVoucherEmailResponse
            {
                TotalSent = sentCount,
                TotalFailed = failedCount,
                Results = results
            };
        }

        public async Task<SendVoucherEmailResponse> SendVoucherToAllUsersAsync(int voucherId, int managerId, SendVoucherToAllRequest request)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền gửi voucher này");
            }

            // Lấy tất cả user có user_type = "User" và is_active = true
            var users = await _context.Users
                .Where(u => u.UserType == "User" && u.IsActive && u.EmailConfirmed)
                .ToListAsync();

            // Convert sang SendVoucherEmailRequest để dùng chung helper method
            var emailRequest = new SendVoucherEmailRequest
            {
                Subject = request.Subject,
                CustomMessage = request.CustomMessage,
                UserIds = null // Không có userIds
            };

            return await SendVoucherEmailsAsync(voucher, users, emailRequest);
        }

        public async Task<List<UserVoucherResponse>> GetValidVouchersForUserAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var vouchers = await _context.Vouchers
                .Include(v => v.Manager)
                .Where(v => !v.IsDeleted &&
                            v.IsActive &&
                            v.ValidFrom <= today &&
                            v.ValidTo >= today &&
                            (v.UsageLimit == null || v.UsedCount < v.UsageLimit))
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new UserVoucherResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    DiscountType = v.DiscountType,
                    DiscountVal = v.DiscountVal,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    Description = v.Description,
                    CreatedAt = v.CreatedAt,
                    DiscountText = v.DiscountType == "percent" ? $"{v.DiscountVal}%" : $"{v.DiscountVal:N0} VNĐ",
                    IsExpired = false,
                    IsAvailable = true,
                    RemainingUses = v.UsageLimit.HasValue ? v.UsageLimit.Value - v.UsedCount : int.MaxValue
                })
                .ToListAsync();

            return vouchers;
        }

        public async Task<VoucherValidationResponse> ValidateVoucherForUserAsync(string voucherCode, decimal totalAmount)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode.ToUpper() &&
                                         !v.IsDeleted);

            if (voucher == null)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Mã voucher không tồn tại",
                    DiscountAmount = 0
                };
            }

            // Kiểm tra trạng thái voucher
            if (!voucher.IsActive)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher đã bị vô hiệu hóa",
                    DiscountAmount = 0
                };
            }

            if (voucher.ValidFrom > today)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher chưa có hiệu lực",
                    DiscountAmount = 0
                };
            }

            if (voucher.ValidTo < today)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher đã hết hạn",
                    DiscountAmount = 0
                };
            }

            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher đã hết lượt sử dụng",
                    DiscountAmount = 0
                };
            }

            // Tính toán số tiền giảm giá
            decimal discountAmount = 0;

            if (voucher.DiscountType == "fixed")
            {
                discountAmount = voucher.DiscountVal;
            }
            else if (voucher.DiscountType == "percent")
            {
                discountAmount = totalAmount * (voucher.DiscountVal / 100);
            }

            // Đảm bảo discount không vượt quá total amount
            if (discountAmount > totalAmount)
            {
                discountAmount = totalAmount;
            }

            var voucherResponse = new UserVoucherResponse
            {
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                DiscountType = voucher.DiscountType,
                DiscountVal = voucher.DiscountVal,
                ValidFrom = voucher.ValidFrom,
                ValidTo = voucher.ValidTo,
                UsageLimit = voucher.UsageLimit,
                UsedCount = voucher.UsedCount,
                Description = voucher.Description,
                CreatedAt = voucher.CreatedAt,
                DiscountText = voucher.DiscountType == "percent" ? $"{voucher.DiscountVal}%" : $"{voucher.DiscountVal:N0} VNĐ",
                IsExpired = false,
                IsAvailable = true,
                RemainingUses = voucher.UsageLimit.HasValue ? voucher.UsageLimit.Value - voucher.UsedCount : int.MaxValue
            };

            return new VoucherValidationResponse
            {
                IsValid = true,
                Message = "Voucher hợp lệ",
                Voucher = voucherResponse,
                DiscountAmount = discountAmount
            };
        }

        public async Task<UserVoucherResponse?> GetVoucherByCodeAsync(string voucherCode)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var voucher = await _context.Vouchers
                .Include(v => v.Manager)
                .Where(v => v.VoucherCode == voucherCode.ToUpper() &&
                           !v.IsDeleted &&
                           v.IsActive &&
                           v.ValidFrom <= today &&
                           v.ValidTo >= today &&
                           (v.UsageLimit == null || v.UsedCount < v.UsageLimit))
                .Select(v => new UserVoucherResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    DiscountType = v.DiscountType,
                    DiscountVal = v.DiscountVal,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    Description = v.Description,
                    CreatedAt = v.CreatedAt,
                    DiscountText = v.DiscountType == "percent" ? $"{v.DiscountVal}%" : $"{v.DiscountVal:N0} VNĐ",
                    IsExpired = false,
                    IsAvailable = true,
                    RemainingUses = v.UsageLimit.HasValue ? v.UsageLimit.Value - v.UsedCount : int.MaxValue
                })
                .FirstOrDefaultAsync();

            return voucher;
        }

        // Trong class VoucherService - THÊM PHƯƠNG THỨC MỚI
        public async Task<UserVoucherResponse?> GetVoucherByIdForUserAsync(int voucherId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var voucher = await _context.Vouchers
                .Include(v => v.Manager)
                .Where(v => v.VoucherId == voucherId &&
                           !v.IsDeleted &&
                           v.IsActive &&
                           v.ValidFrom <= today &&
                           v.ValidTo >= today &&
                           (v.UsageLimit == null || v.UsedCount < v.UsageLimit))
                .Select(v => new UserVoucherResponse
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    DiscountType = v.DiscountType,
                    DiscountVal = v.DiscountVal,
                    ValidFrom = v.ValidFrom,
                    ValidTo = v.ValidTo,
                    UsageLimit = v.UsageLimit,
                    UsedCount = v.UsedCount,
                    Description = v.Description,
                    CreatedAt = v.CreatedAt,
                    DiscountText = v.DiscountType == "percent" ? $"{v.DiscountVal}%" : $"{v.DiscountVal:N0} VNĐ",
                    IsExpired = false,
                    IsAvailable = true,
                    RemainingUses = v.UsageLimit.HasValue ? v.UsageLimit.Value - v.UsedCount : int.MaxValue
                })
                .FirstOrDefaultAsync();

            return voucher;
        }
    }
}