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
        Task<VoucherResponse> CreateVoucherAsync(int managerId, CreateVoucherRequest request, int? managerStaffId = null);
        Task<PaginatedVouchersResponse> GetAllVouchersAsync(int managerId, int page, int limit, string? search, string? status, string? sortBy, string? sortOrder);
        Task<VoucherResponse> GetVoucherByIdAsync(int voucherId, int managerId);
        Task<VoucherResponse> UpdateVoucherAsync(int voucherId, int managerId, UpdateVoucherRequest request, int? managerStaffId = null);
        Task SoftDeleteVoucherAsync(int voucherId, int managerId);
        Task ToggleVoucherStatusAsync(int voucherId, int managerId);
        Task<List<VoucherEmailHistoryResponse>> GetVoucherEmailHistoryAsync(int voucherId, int managerId);
        Task<bool> ValidateVoucherCodeAsync(string voucherCode, int? excludeVoucherId = null);

        // THÊM MỚI - Gửi voucher cho tất cả users
        Task<SendVoucherEmailResponse> SendVoucherToAllUsersAsync(int voucherId, int managerId, SendVoucherToAllRequest request);

        // THÊM MỚI - Gửi voucher cho users cụ thể
        Task<SendVoucherEmailResponse> SendVoucherToSpecificUsersAsync(int voucherId, int managerId, SendVoucherEmailRequest request);

        // THÊM MỚI - Gửi voucher cho top khách hàng mua nhiều nhất
        Task<SendVoucherEmailResponse> SendVoucherToTopBuyersAsync(int voucherId, int managerId, SendVoucherToTopBuyersRequest request);

        // THÊM MỚI - Gửi voucher cho top khách hàng chi tiêu nhiều nhất
        Task<SendVoucherEmailResponse> SendVoucherToTopSpendersAsync(int voucherId, int managerId, SendVoucherToTopSpendersRequest request);

        Task<List<UserVoucherResponse>> GetValidVouchersForUserAsync();

        // THÊM MỚI - Validate voucher khi user sử dụng
        Task<VoucherValidationResponse> ValidateVoucherForUserAsync(string voucherCode, decimal totalAmount, int? userId = null);

        // THÊM MỚI - Reserve voucher cho session (tránh race condition)
        Task<VoucherValidationResponse> ReserveVoucherForSessionAsync(string voucherCode, decimal totalAmount, Guid sessionId, int userId, DateTime sessionExpiresAt);

        // THÊM MỚI - Release voucher reservation
        Task ReleaseVoucherReservationAsync(Guid sessionId);

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
        private readonly IAuditLogService _auditLogService;

        public VoucherService(
            CinemaDbCoreContext context,
            IManagerService managerService,
            IEmailService emailService,
            ILogger<VoucherService> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _managerService = managerService;
            _emailService = emailService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<VoucherResponse> CreateVoucherAsync(int managerId, CreateVoucherRequest request, int? managerStaffId = null)
        {
            // Validate manager exists
            if (!await _managerService.ValidateManagerExistsAsync(managerId))
            {
                throw new NotFoundException("Manager không tồn tại");
            }

            // If ManagerStaff creates voucher, verify they belong to this Manager and have permission
            if (managerStaffId.HasValue)
            {
                var managerStaff = await _context.ManagerStaffs
                    .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId.Value && ms.ManagerId == managerId && ms.IsActive);
                if (managerStaff == null)
                {
                    throw new UnauthorizedException(new Dictionary<string, ValidationError>
                    {
                        ["managerStaff"] = new ValidationError
                        {
                            Msg = "ManagerStaff không thuộc quyền quản lý của Manager này",
                            Path = "managerStaffId",
                            Location = "body"
                        }
                    });
                }
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
                ManagerStaffId = managerStaffId, // Set ManagerStaffId if created by ManagerStaff
                IsRestricted = request.IsRestricted,
                ManagerId = managerId,
                CreatedAt = DateTime.UtcNow,
                UsedCount = 0,
                IsDeleted = false
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_CREATE_VOUCHER",
                tableName: "Voucher",
                recordId: voucher.VoucherId,
                beforeData: null,
                afterData: BuildVoucherSnapshot(voucher),
                metadata: new { managerId });

            // Reload voucher with navigation properties for response
            var createdVoucher = await _context.Vouchers
                .Include(v => v.Manager)
                .ThenInclude(m => m.User)
                .Include(v => v.ManagerStaff)
                .ThenInclude(ms => ms.User)
                .FirstOrDefaultAsync(v => v.VoucherId == voucher.VoucherId);

            return await MapToVoucherResponseAsync(createdVoucher!);
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
                    IsRestricted = v.IsRestricted,
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
                .Include(v => v.ManagerStaff)
                .ThenInclude(ms => ms.User)
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

        public async Task<VoucherResponse> UpdateVoucherAsync(int voucherId, int managerId, UpdateVoucherRequest request, int? managerStaffId = null)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Manager)
                .ThenInclude(m => m.User)
                .Include(v => v.ManagerStaff)
                .ThenInclude(ms => ms.User)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId && !v.IsDeleted);

            if (voucher == null)
            {
                throw new NotFoundException("Voucher không tồn tại");
            }

            if (voucher.ManagerId != managerId)
            {
                throw new UnauthorizedException("Bạn không có quyền cập nhật voucher này");
            }

            // If ManagerStaff updates voucher, update ManagerStaffId
            if (managerStaffId.HasValue)
            {
                voucher.ManagerStaffId = managerStaffId;
            }

            var beforeSnapshot = BuildVoucherSnapshot(voucher);

            // ✅ THÊM VALIDATE: Kiểm tra voucher đã được gửi chưa
            bool hasBeenSent = await HasVoucherBeenSentAsync(voucherId);
            if (hasBeenSent)
            {
                // Nếu voucher đã gửi, chỉ cho phép cập nhật một số field
                var allowedUpdates = new List<string>();

                if (!string.IsNullOrEmpty(request.VoucherCode) && request.VoucherCode != voucher.VoucherCode)
                {
                    throw new ValidationException("VoucherCode", "Không thể thay đổi mã voucher sau khi đã gửi cho users");
                }

                if (!string.IsNullOrEmpty(request.DiscountType) && request.DiscountType != voucher.DiscountType)
                {
                    throw new ValidationException("DiscountType", "Không thể thay đổi loại giảm giá sau khi đã gửi cho users");
                }

                if (request.DiscountVal.HasValue && request.DiscountVal.Value != voucher.DiscountVal)
                {
                    throw new ValidationException("DiscountVal", "Không thể thay đổi giá trị giảm giá sau khi đã gửi cho users");
                }

                if (request.ValidFrom.HasValue && request.ValidFrom.Value != voucher.ValidFrom)
                {
                    throw new ValidationException("ValidFrom", "Không thể thay đổi ngày bắt đầu sau khi đã gửi cho users");
                }

                if (request.IsRestricted.HasValue && request.IsRestricted.Value != voucher.IsRestricted)
                {
                    throw new ValidationException("IsRestricted", "Không thể thay đổi loại voucher (Public/Restricted) sau khi đã gửi cho users");
                }

                // Cho phép cập nhật các field sau ngay cả khi đã gửi
                if (request.ValidTo.HasValue)
                    voucher.ValidTo = request.ValidTo.Value;

                if (request.UsageLimit.HasValue)
                    voucher.UsageLimit = request.UsageLimit.Value;

                if (request.Description != null)
                    voucher.Description = request.Description;

                if (request.IsActive.HasValue)
                    voucher.IsActive = request.IsActive.Value;
            }
            else
            {
                // ❌ Voucher chưa gửi - cho phép cập nhật tất cả (logic cũ)
                if (!string.IsNullOrEmpty(request.VoucherCode) && request.VoucherCode != voucher.VoucherCode)
                {
                    if (await ValidateVoucherCodeAsync(request.VoucherCode, voucherId))
                    {
                        throw new ValidationException("VoucherCode", "Mã voucher đã tồn tại");
                    }
                    voucher.VoucherCode = request.VoucherCode.ToUpper();
                }

                if (!string.IsNullOrEmpty(request.DiscountType))
                    voucher.DiscountType = request.DiscountType;

                if (request.DiscountVal.HasValue)
                {
                    if ((request.DiscountType ?? voucher.DiscountType) == "percent" && request.DiscountVal > 100)
                    {
                        throw new ValidationException("DiscountVal", "Giá trị giảm giá phần trăm không được vượt quá 100");
                    }
                    voucher.DiscountVal = request.DiscountVal.Value;
                }

                if (request.ValidFrom.HasValue)
                {
                    if (request.ValidFrom.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                    {
                        throw new ValidationException("ValidFrom", "Ngày bắt đầu không được ở quá khứ");
                    }
                    voucher.ValidFrom = request.ValidFrom.Value;
                }

                if (request.ValidTo.HasValue)
                    voucher.ValidTo = request.ValidTo.Value;

                if (request.UsageLimit.HasValue)
                    voucher.UsageLimit = request.UsageLimit.Value;

                if (request.Description != null)
                    voucher.Description = request.Description;

                if (request.IsActive.HasValue)
                    voucher.IsActive = request.IsActive.Value;

                if (request.IsRestricted.HasValue)
                    voucher.IsRestricted = request.IsRestricted.Value;
            }

            // Validate dates sau khi update
            if (voucher.ValidTo <= voucher.ValidFrom)
            {
                throw new ValidationException("ValidTo", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            voucher.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_UPDATE_VOUCHER",
                tableName: "Voucher",
                recordId: voucher.VoucherId,
                beforeData: beforeSnapshot,
                afterData: BuildVoucherSnapshot(voucher),
                metadata: new { managerId, voucherId });

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

            if (await HasVoucherBeenSentAsync(voucherId))
            {
                throw new ValidationException("Voucher", "Không thể xóa voucher đã được gửi cho users");
            }

            var beforeSnapshot = BuildVoucherSnapshot(voucher);
            voucher.IsDeleted = true;
            voucher.DeletedAt = DateTime.UtcNow;
            voucher.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_DELETE_VOUCHER",
                tableName: "Voucher",
                recordId: voucher.VoucherId,
                beforeData: beforeSnapshot,
                afterData: BuildVoucherSnapshot(voucher),
                metadata: new { managerId, voucherId });
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

            if (!voucher.IsActive && await HasVoucherBeenSentAsync(voucherId))
            {
                throw new ValidationException("Voucher", "Không thể vô hiệu hóa voucher đã được gửi cho users");
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
                IsRestricted = voucher.IsRestricted,
                CreatedAt = voucher.CreatedAt,
                UpdatedAt = voucher.UpdatedAt,
                ManagerId = voucher.ManagerId,
                ManagerName = voucher.Manager.FullName,
                ManagerStaffId = voucher.ManagerStaffId,
                ManagerStaffName = voucher.ManagerStaff?.FullName
            };
        }

        private static object BuildVoucherSnapshot(Voucher voucher) => new
        {
            voucher.VoucherId,
            voucher.ManagerId,
            voucher.VoucherCode,
            voucher.DiscountType,
            voucher.DiscountVal,
            voucher.ValidFrom,
            voucher.ValidTo,
            voucher.UsageLimit,
            voucher.UsedCount,
            voucher.Description,
            voucher.IsActive,
            voucher.IsRestricted,
            voucher.IsDeleted,
            voucher.CreatedAt,
            voucher.UpdatedAt
        };

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

            var response = await SendVoucherEmailsAsync(voucher, users, request);
            await LogVoucherCampaignAsync("MANAGER_VOUCHER_SEND_SPECIFIC", voucher, response, new
            {
                targetUserCount = users.Count
            });
            return response;
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

            var response = await SendVoucherEmailsAsync(voucher, users, request);
            await LogVoucherCampaignAsync("MANAGER_VOUCHER_SEND_ALL", voucher, response, new
            {
                targetUserCount = users.Count
            });
            return response;
        }

        // Phương thức helper để gửi email
        private async Task<SendVoucherEmailResponse> SendVoucherEmailsAsync(Voucher voucher, List<User> users, SendVoucherEmailRequest request)
        {
            var results = new List<EmailSendResult>();
            var sentCount = 0;
            var failedCount = 0;

            // ✅ THÊM: Lấy danh sách user đã nhận voucher này
            var existingSentUserIds = await _context.VoucherEmailHistories
                .Where(h => h.VoucherId == voucher.VoucherId && h.Status == "success")
                .Select(h => h.UserId)
                .ToListAsync();

            // ✅ Lọc ra chỉ những user CHƯA nhận voucher này
            var usersToSend = users.Where(u => !existingSentUserIds.Contains(u.UserId)).ToList();

            if (!usersToSend.Any())
            {
                throw new ValidationException("Users", "Tất cả users đã nhận voucher này trước đó");
            }

            // ✅ THÊM MỚI: Nếu voucher là Restricted, tạo UserVoucher records cho tất cả users được gửi
            if (voucher.IsRestricted)
            {
                foreach (var user in usersToSend)
                {
                    // Kiểm tra xem đã có UserVoucher record chưa (tránh duplicate)
                    var existingUserVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv => uv.VoucherId == voucher.VoucherId && uv.UserId == user.UserId);

                    if (existingUserVoucher == null)
                    {
                        var userVoucher = new UserVoucher
                        {
                            VoucherId = voucher.VoucherId,
                            UserId = user.UserId,
                            IsUsed = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.UserVouchers.Add(userVoucher);
                    }
                }
            }

            foreach (var user in usersToSend)
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

        private Task LogVoucherCampaignAsync(string action, Voucher voucher, SendVoucherEmailResponse response, object? extra = null) =>
            _auditLogService.LogEntityChangeAsync(
                action: action,
                tableName: "Voucher",
                recordId: voucher.VoucherId,
                beforeData: null,
                afterData: new
                {
                    voucher.VoucherId,
                    campaign = action,
                    sent = response.TotalSent,
                    failed = response.TotalFailed,
                    executedAt = DateTime.UtcNow,
                    extra
                },
                metadata: new { voucher.ManagerId });

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

            var response = await SendVoucherEmailsAsync(voucher, users, emailRequest);
            await LogVoucherCampaignAsync("MANAGER_VOUCHER_SEND_ALL", voucher, response, new
            {
                targetUserCount = users.Count
            });
            return response;
        }

        public async Task<SendVoucherEmailResponse> SendVoucherToTopBuyersAsync(int voucherId, int managerId, SendVoucherToTopBuyersRequest request)
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

            // Tính toán ngày bắt đầu nếu có periodDays
            var startDate = request.PeriodDays.HasValue
                ? DateTime.UtcNow.AddDays(-request.PeriodDays.Value)
                : (DateTime?)null;

            // Query bookings với các điều kiện
            var bookingsQuery = _context.Bookings
                .AsQueryable();

            // Lọc theo onlyPaidBookings
            if (request.OnlyPaidBookings)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Status == "CONFIRMED" && b.PaymentStatus == "PAID");
            }

            // Lọc theo periodDays nếu có
            if (startDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BookingTime >= startDate.Value);
            }

            // Group by CustomerId và đếm số lượng booking, join với Customer để lấy UserId
            var topBuyersQuery = bookingsQuery
                .Join(_context.Customers,
                    booking => booking.CustomerId,
                    customer => customer.CustomerId,
                    (booking, customer) => new { Booking = booking, Customer = customer })
                .GroupBy(x => x.Customer.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    BookingCount = g.Count(),
                    UserId = g.First().Customer.UserId
                });

            // Lọc theo minBookingCount nếu có
            if (request.MinBookingCount.HasValue)
            {
                topBuyersQuery = topBuyersQuery.Where(x => x.BookingCount >= request.MinBookingCount.Value);
            }

            // Order by booking count DESC và lấy top limit
            var topBuyers = await topBuyersQuery
                .OrderByDescending(x => x.BookingCount)
                .Take(request.TopLimit)
                .Select(x => x.UserId)
                .ToListAsync();

            if (!topBuyers.Any())
            {
                throw new ValidationException("TopBuyers", "Không tìm thấy khách hàng nào thỏa mãn điều kiện");
            }

            // Lấy users với các điều kiện: IsActive = true, EmailConfirmed = true, UserType = "User"
            var users = await _context.Users
                .Where(u => topBuyers.Contains(u.UserId) &&
                           u.UserType == "User" &&
                           u.IsActive &&
                           u.EmailConfirmed)
                .ToListAsync();

            if (!users.Any())
            {
                throw new ValidationException("Users", "Không tìm thấy users hợp lệ để gửi voucher");
            }

            // Convert sang SendVoucherEmailRequest để dùng chung helper method
            var emailRequest = new SendVoucherEmailRequest
            {
                Subject = request.Subject,
                CustomMessage = request.CustomMessage,
                UserIds = null
            };

            var response = await SendVoucherEmailsAsync(voucher, users, emailRequest);
            await LogVoucherCampaignAsync("MANAGER_VOUCHER_SEND_TOP_BUYERS", voucher, response, new
            {
                request.TopLimit,
                request.PeriodDays,
                request.MinBookingCount
            });
            return response;
        }

        public async Task<SendVoucherEmailResponse> SendVoucherToTopSpendersAsync(int voucherId, int managerId, SendVoucherToTopSpendersRequest request)
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

            // Tính toán ngày bắt đầu nếu có periodDays
            var startDate = request.PeriodDays.HasValue
                ? DateTime.UtcNow.AddDays(-request.PeriodDays.Value)
                : (DateTime?)null;

            // Query bookings với các điều kiện
            var bookingsQuery = _context.Bookings
                .AsQueryable();

            // Lọc theo onlyPaidBookings
            if (request.OnlyPaidBookings)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Status == "CONFIRMED" && b.PaymentStatus == "PAID");
            }

            // Lọc theo periodDays nếu có
            if (startDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BookingTime >= startDate.Value);
            }

            // Group by CustomerId và sum TotalAmount, join với Customer để lấy UserId
            var topSpendersQuery = bookingsQuery
                .Join(_context.Customers,
                    booking => booking.CustomerId,
                    customer => customer.CustomerId,
                    (booking, customer) => new { Booking = booking, Customer = customer })
                .GroupBy(x => x.Customer.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalSpent = g.Sum(x => x.Booking.TotalAmount),
                    UserId = g.First().Customer.UserId
                });

            // Lọc theo minTotalSpent nếu có
            if (request.MinTotalSpent.HasValue)
            {
                topSpendersQuery = topSpendersQuery.Where(x => x.TotalSpent >= request.MinTotalSpent.Value);
            }

            // Order by total spent DESC và lấy top limit
            var topSpenders = await topSpendersQuery
                .OrderByDescending(x => x.TotalSpent)
                .Take(request.TopLimit)
                .Select(x => x.UserId)
                .ToListAsync();

            if (!topSpenders.Any())
            {
                throw new ValidationException("TopSpenders", "Không tìm thấy khách hàng nào thỏa mãn điều kiện");
            }

            // Lấy users với các điều kiện: IsActive = true, EmailConfirmed = true, UserType = "User"
            var users = await _context.Users
                .Where(u => topSpenders.Contains(u.UserId) &&
                           u.UserType == "User" &&
                           u.IsActive &&
                           u.EmailConfirmed)
                .ToListAsync();

            if (!users.Any())
            {
                throw new ValidationException("Users", "Không tìm thấy users hợp lệ để gửi voucher");
            }

            // Convert sang SendVoucherEmailRequest để dùng chung helper method
            var emailRequest = new SendVoucherEmailRequest
            {
                Subject = request.Subject,
                CustomMessage = request.CustomMessage,
                UserIds = null
            };

            var response = await SendVoucherEmailsAsync(voucher, users, emailRequest);
            await LogVoucherCampaignAsync("MANAGER_VOUCHER_SEND_TOP_SPENDERS", voucher, response, new
            {
                request.TopLimit,
                request.PeriodDays,
                request.MinTotalSpent
            });
            return response;
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

        public async Task<VoucherValidationResponse> ValidateVoucherForUserAsync(string voucherCode, decimal totalAmount, int? userId = null)
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

            // ✅ THÊM MỚI: Kiểm tra voucher có phải Restricted không
            if (voucher.IsRestricted)
            {
                // Restricted voucher: Chỉ user được gửi email mới dùng được
                if (!userId.HasValue)
                {
                    return new VoucherValidationResponse
                    {
                        IsValid = false,
                        Message = "Bạn cần đăng nhập để sử dụng voucher này",
                        DiscountAmount = 0
                    };
                }

                var userVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.VoucherId == voucher.VoucherId && uv.UserId == userId.Value);

                if (userVoucher == null)
                {
                    return new VoucherValidationResponse
                    {
                        IsValid = false,
                        Message = "Bạn không có quyền sử dụng voucher này. Voucher này chỉ dành cho khách hàng được chọn.",
                        DiscountAmount = 0
                    };
                }

                if (userVoucher.IsUsed)
                {
                    return new VoucherValidationResponse
                    {
                        IsValid = false,
                        Message = "Bạn đã sử dụng voucher này rồi. Mỗi tài khoản chỉ được sử dụng voucher 1 lần.",
                        DiscountAmount = 0
                    };
                }
            }
            else
            {
                // Public voucher: Ai cũng dùng được, chỉ cần check user đã dùng chưa
                if (userId.HasValue)
                {
                    var userVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv => uv.VoucherId == voucher.VoucherId && uv.UserId == userId.Value);

                    if (userVoucher != null && userVoucher.IsUsed)
                    {
                        return new VoucherValidationResponse
                        {
                            IsValid = false,
                            Message = "Bạn đã sử dụng voucher này rồi. Mỗi tài khoản chỉ được sử dụng voucher 1 lần.",
                            DiscountAmount = 0
                        };
                    }
                }
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
        private async Task<bool> HasVoucherBeenSentAsync(int voucherId)
        {
            return await _context.VoucherEmailHistories
                .AnyAsync(h => h.VoucherId == voucherId && h.Status == "success");
        }

        private async Task<bool> HasUserReceivedVoucherAsync(int voucherId, int userId)
        {
            return await _context.VoucherEmailHistories
                .AnyAsync(h => h.VoucherId == voucherId && h.UserId == userId && h.Status == "success");
        }

        // ✅ THÊM MỚI: Reserve voucher cho session
        public async Task<VoucherValidationResponse> ReserveVoucherForSessionAsync(string voucherCode, decimal totalAmount, Guid sessionId, int userId, DateTime sessionExpiresAt)
        {
            var now = DateTime.UtcNow;

            // 1. Validate voucher cơ bản trước
            var validation = await ValidateVoucherForUserAsync(voucherCode, totalAmount, userId);
            if (!validation.IsValid)
            {
                return validation;
            }

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode.ToUpper() && !v.IsDeleted);

            if (voucher == null)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Mã voucher không tồn tại",
                    DiscountAmount = 0
                };
            }

            // ✅ 2. CHỈ reserve nếu voucher còn đúng 1 lượt sử dụng (giftcode đặc biệt cho 1 người)
            // Nếu voucher còn nhiều lượt (ví dụ: 0/100) thì không cần reserve, cho phép nhiều người cùng dùng
            bool needsReservation = false;
            if (voucher.UsageLimit.HasValue)
            {
                var remainingUses = voucher.UsageLimit.Value - voucher.UsedCount;
                const int reservationThreshold = 1; // Chỉ reserve khi còn đúng 1 lượt (giftcode đặc biệt)
                needsReservation = remainingUses <= reservationThreshold;
            }
            else
            {
                // Voucher không giới hạn lượt sử dụng → không cần reserve
                needsReservation = false;
            }

            // Nếu không cần reserve, chỉ return validation result
            if (!needsReservation)
            {
                return validation;
            }

            // 3. Kiểm tra voucher có đang được reserve bởi session khác không (chỉ khi cần reserve)
            var existingReservation = await _context.VoucherReservations
                .Where(r => r.VoucherId == voucher.VoucherId 
                         && r.SessionId != sessionId 
                         && r.ReleasedAt == null 
                         && r.ExpiresAt > now)
                .FirstOrDefaultAsync();

            if (existingReservation != null)
            {
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher đang được sử dụng bởi session khác. Vui lòng đợi hoặc thử lại sau.",
                    DiscountAmount = 0
                };
            }

            // 3. Sử dụng transaction với isolation level cao để đảm bảo atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                // 4. Check lại với lock để tránh race condition
                // Sử dụng transaction isolation level Serializable để lock row
                var lockedReservation = await _context.VoucherReservations
                    .Where(r => r.VoucherId == voucher.VoucherId 
                             && r.ReleasedAt == null 
                             && r.ExpiresAt > now)
                    .FirstOrDefaultAsync();

                if (lockedReservation != null && lockedReservation.SessionId != sessionId)
                {
                    await transaction.RollbackAsync();
                    return new VoucherValidationResponse
                    {
                        IsValid = false,
                        Message = "Voucher đang được sử dụng bởi session khác. Vui lòng đợi hoặc thử lại sau.",
                        DiscountAmount = 0
                    };
                }

                // 5. Release reservation cũ của session này (nếu có)
                var oldReservation = await _context.VoucherReservations
                    .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.ReleasedAt == null);

                if (oldReservation != null)
                {
                    oldReservation.ReleasedAt = now;
                }

                // 6. Tạo reservation mới
                var reservation = new VoucherReservation
                {
                    VoucherId = voucher.VoucherId,
                    SessionId = sessionId,
                    UserId = userId,
                    ReservedAt = now,
                    ExpiresAt = sessionExpiresAt
                };

                _context.VoucherReservations.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Return validation response với discount amount
                return validation;
            }
            catch (DbUpdateException ex)
            {
                // Unique constraint violation hoặc lỗi khác - voucher đã được reserve bởi session khác
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Error reserving voucher {VoucherCode} for session {SessionId} - possibly unique constraint violation", voucherCode, sessionId);
                return new VoucherValidationResponse
                {
                    IsValid = false,
                    Message = "Voucher đang được sử dụng bởi session khác. Vui lòng đợi hoặc thử lại sau.",
                    DiscountAmount = 0
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reserving voucher {VoucherCode} for session {SessionId}", voucherCode, sessionId);
                throw;
            }
        }

        // ✅ THÊM MỚI: Release voucher reservation
        public async Task ReleaseVoucherReservationAsync(Guid sessionId)
        {
            var now = DateTime.UtcNow;
            var reservations = await _context.VoucherReservations
                .Where(r => r.SessionId == sessionId && r.ReleasedAt == null)
                .ToListAsync();

            if (reservations.Any())
            {
                foreach (var reservation in reservations)
                {
                    reservation.ReleasedAt = now;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}