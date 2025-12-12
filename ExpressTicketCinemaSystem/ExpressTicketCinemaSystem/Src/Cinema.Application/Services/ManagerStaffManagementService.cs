using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ManagerStaffManagementService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;

        public ManagerStaffManagementService(
            CinemaDbCoreContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public async Task<ManagerStaffResponse> CreateManagerStaffAsync(int managerId, CreateManagerStaffRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validation
            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new ValidationError { Msg = "Họ và tên là bắt buộc", Path = "fullName" };

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["email"] = new ValidationError { Msg = "Email là bắt buộc", Path = "email" };
            else if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = new ValidationError { Msg = "Định dạng email không hợp lệ", Path = "email" };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new ValidationError { Msg = "Mật khẩu là bắt buộc", Path = "password" };
            else if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,12}$"))
                errors["password"] = new ValidationError { Msg = "Mật khẩu phải từ 6-12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt", Path = "password" };

            if (request.Password != request.ConfirmPassword)
                errors["confirmPassword"] = new ValidationError { Msg = "Mật khẩu và xác nhận mật khẩu không khớp", Path = "confirmPassword" };

            var validRoles = new[] { "ManagerStaff" };
            if (string.IsNullOrWhiteSpace(request.RoleType) || !validRoles.Contains(request.RoleType))
                errors["roleType"] = new ValidationError { Msg = "RoleType phải là ManagerStaff", Path = "roleType" };

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new ConflictException("email", "Email đã tồn tại");

            // Check if user already has a manager staff record
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null && await _context.ManagerStaffs.AnyAsync(ms => ms.UserId == existingUser.UserId))
                throw new ConflictException("email", "Người dùng này đã là staff của manager");

            if (errors.Any())
                throw new ValidationException(errors);

            // Create User
            var user = new User
            {
                Fullname = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Username = request.Email,
                UserType = request.RoleType,
                Password = _passwordHasher.HashPassword(null, request.Password),
                IsActive = true,
                EmailConfirmed = true, // Manager creates staff, so email is considered confirmed
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create ManagerStaff
            var managerStaff = new ManagerStaff
            {
                ManagerId = managerId,
                UserId = user.UserId,
                FullName = request.FullName,
                RoleType = request.RoleType,
                HireDate = request.HireDate,
                IsActive = true
            };

            _context.ManagerStaffs.Add(managerStaff);
            await _context.SaveChangesAsync();
            managerStaff.User = user;
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_CREATE_STAFF",
                tableName: "ManagerStaff",
                recordId: managerStaff.ManagerStaffId,
                beforeData: null,
                afterData: BuildManagerStaffSnapshot(managerStaff),
                metadata: new { managerId });

            return new ManagerStaffResponse
            {
                ManagerStaffId = managerStaff.ManagerStaffId,
                ManagerId = managerStaff.ManagerId,
                UserId = user.UserId,
                FullName = managerStaff.FullName,
                Email = user.Email,
                Phone = user.Phone ?? "",
                RoleType = managerStaff.RoleType,
                HireDate = managerStaff.HireDate,
                IsActive = managerStaff.IsActive
            };
        }

        public async Task<ManagerStaffResponse> UpdateManagerStaffAsync(int managerId, int managerStaffId, UpdateManagerStaffRequest request)
        {
            var managerStaff = await _context.ManagerStaffs
                .Include(ms => ms.User)
                .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId);

            if (managerStaff == null)
                throw new NotFoundException("Không tìm thấy staff");

            var errors = new Dictionary<string, ValidationError>();
            var beforeSnapshot = BuildManagerStaffSnapshot(managerStaff);

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                managerStaff.FullName = request.FullName;
                managerStaff.User.Fullname = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
                managerStaff.User.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.RoleType))
            {
                var validRoles = new[] { "ManagerStaff" };
                if (!validRoles.Contains(request.RoleType))
                    errors["roleType"] = new ValidationError { Msg = "RoleType phải là ManagerStaff", Path = "roleType" };
                else
                {
                    managerStaff.RoleType = request.RoleType;
                    managerStaff.User.UserType = request.RoleType;
                }
            }

            if (request.IsActive.HasValue)
            {
                managerStaff.IsActive = request.IsActive.Value;
                managerStaff.User.IsActive = request.IsActive.Value;
            }

            if (errors.Any())
                throw new ValidationException(errors);

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_UPDATE_STAFF",
                tableName: "ManagerStaff",
                recordId: managerStaff.ManagerStaffId,
                beforeData: beforeSnapshot,
                afterData: BuildManagerStaffSnapshot(managerStaff),
                metadata: new { managerId });

            return new ManagerStaffResponse
            {
                ManagerStaffId = managerStaff.ManagerStaffId,
                ManagerId = managerStaff.ManagerId,
                UserId = managerStaff.UserId,
                FullName = managerStaff.FullName,
                Email = managerStaff.User.Email,
                Phone = managerStaff.User.Phone ?? "",
                RoleType = managerStaff.RoleType,
                HireDate = managerStaff.HireDate,
                IsActive = managerStaff.IsActive
            };
        }

        public async Task<PaginatedManagerStaffsResponse> GetManagerStaffsAsync(
            int managerId,
            int page = 1,
            int limit = 10,
            bool? isActive = null,
            string? search = null,
            string sortBy = "fullName",
            string sortOrder = "asc")
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _context.ManagerStaffs
                .Include(ms => ms.User)
                .Where(ms => ms.ManagerId == managerId);

            if (isActive.HasValue)
                query = query.Where(ms => ms.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(ms =>
                    ms.FullName.Contains(search) ||
                    ms.User.Email.Contains(search) ||
                    (ms.User.Phone != null && ms.User.Phone.Contains(search)));
            }

            // Sorting
            query = sortBy.ToLower() switch
            {
                "fullname" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(ms => ms.FullName) : query.OrderBy(ms => ms.FullName),
                "email" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(ms => ms.User.Email) : query.OrderBy(ms => ms.User.Email),
                "roletype" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(ms => ms.RoleType) : query.OrderBy(ms => ms.RoleType),
                "hiredate" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(ms => ms.HireDate) : query.OrderBy(ms => ms.HireDate),
                _ => query.OrderBy(ms => ms.FullName)
            };

            var total = await query.CountAsync();
            var managerStaffs = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(ms => new ManagerStaffResponse
                {
                    ManagerStaffId = ms.ManagerStaffId,
                    ManagerId = ms.ManagerId,
                    UserId = ms.UserId,
                    FullName = ms.FullName,
                    Email = ms.User.Email,
                    Phone = ms.User.Phone ?? "",
                    RoleType = ms.RoleType,
                    HireDate = ms.HireDate,
                    IsActive = ms.IsActive
                })
                .ToListAsync();

            return new PaginatedManagerStaffsResponse
            {
                ManagerStaffs = managerStaffs,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = limit,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<ManagerStaffResponse> GetManagerStaffByIdAsync(int managerId, int managerStaffId)
        {
            var managerStaff = await _context.ManagerStaffs
                .Include(ms => ms.User)
                .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId);

            if (managerStaff == null)
                throw new NotFoundException("Không tìm thấy staff");

            return new ManagerStaffResponse
            {
                ManagerStaffId = managerStaff.ManagerStaffId,
                ManagerId = managerStaff.ManagerId,
                UserId = managerStaff.UserId,
                FullName = managerStaff.FullName,
                Email = managerStaff.User.Email,
                Phone = managerStaff.User.Phone ?? "",
                RoleType = managerStaff.RoleType,
                HireDate = managerStaff.HireDate,
                IsActive = managerStaff.IsActive
            };
        }

        public async Task DeleteManagerStaffAsync(int managerId, int managerStaffId)
        {
            var managerStaff = await _context.ManagerStaffs
                .Include(ms => ms.User)
                .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId);

            if (managerStaff == null)
                throw new NotFoundException("Không tìm thấy staff");

            var beforeSnapshot = BuildManagerStaffSnapshot(managerStaff);

            // Soft delete: set IsActive = false
            managerStaff.IsActive = false;
            managerStaff.User.IsActive = false;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_DELETE_STAFF",
                tableName: "ManagerStaff",
                recordId: managerStaff.ManagerStaffId,
                beforeData: beforeSnapshot,
                afterData: BuildManagerStaffSnapshot(managerStaff),
                metadata: new { managerId });
        }

        private static object BuildManagerStaffSnapshot(ManagerStaff managerStaff)
        {
            return new
            {
                managerStaff.ManagerStaffId,
                managerStaff.ManagerId,
                managerStaff.UserId,
                managerStaff.FullName,
                managerStaff.RoleType,
                managerStaff.HireDate,
                managerStaff.IsActive,
                User = managerStaff.User == null
                    ? null
                    : new
                    {
                        managerStaff.User.UserId,
                        managerStaff.User.Email,
                        managerStaff.User.Fullname,
                        managerStaff.User.Phone,
                        managerStaff.User.UserType,
                        managerStaff.User.IsActive
                    }
            };
        }

        /// <summary>
        /// Get ManagerStaff profile with assigned partners and permissions
        /// </summary>
        public async Task<ManagerStaffProfileResponse> GetManagerStaffProfileAsync(int userId)
        {
            // Lấy thông tin ManagerStaff từ UserId
            var managerStaff = await _context.ManagerStaffs
                .Include(ms => ms.User)
                .Include(ms => ms.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(ms => ms.UserId == userId && ms.IsActive);

            if (managerStaff == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin manager staff hoặc tài khoản chưa được kích hoạt");
            }

            // Lấy danh sách partners được phân công
            var assignedPartners = await _context.Partners
                .Where(p => p.ManagerStaffId == managerStaff.ManagerStaffId)
                .Select(p => new AssignedPartnerInfo
                {
                    PartnerId = p.PartnerId,
                    PartnerName = p.PartnerName,
                    TaxCode = p.TaxCode,
                    Address = p.Address,
                    Status = p.Status ?? "pending",
                    AssignedAt = p.UpdatedAt ?? p.CreatedAt, // Sử dụng UpdatedAt hoặc CreatedAt làm AssignedAt
                    AssignedByUserId = null, // Có thể thêm field này vào Partner table nếu cần
                    AssignedByEmail = null,
                    AssignedByName = null
                })
                .ToListAsync();

            // Lấy danh sách permissions - sử dụng ManagerStaffPermissionService
            var permissionsListResponse = await GetManagerStaffPermissionsForProfileAsync(managerStaff.ManagerStaffId);

            // Convert từ PartnerPermissionsGroup sang GrantedPermissionInfo
            var grantedPermissions = new List<GrantedPermissionInfo>();
            foreach (var partnerGroup in permissionsListResponse.PartnerPermissions)
            {
                foreach (var perm in partnerGroup.Permissions)
                {
                    grantedPermissions.Add(new GrantedPermissionInfo
                    {
                        PermissionId = perm.PermissionId,
                        PermissionCode = perm.PermissionCode,
                        PermissionName = perm.PermissionName,
                        ResourceType = perm.ResourceType,
                        ActionType = perm.ActionType,
                        Description = perm.Description,
                        PartnerId = partnerGroup.PartnerId,
                        PartnerName = partnerGroup.PartnerName,
                        GrantedAt = perm.GrantedAt,
                        GrantedByUserId = perm.GrantedByUserId,
                        GrantedByEmail = perm.GrantedByEmail,
                        GrantedByName = perm.GrantedByName,
                        IsActive = perm.IsActive
                    });
                }
            }

            return new ManagerStaffProfileResponse
            {
                ManagerStaffId = managerStaff.ManagerStaffId,
                FullName = managerStaff.FullName,
                Email = managerStaff.User.Email,
                Phone = managerStaff.User.Phone,
                RoleType = managerStaff.RoleType ?? "ManagerStaff",
                IsActive = managerStaff.IsActive,
                ManagerId = managerStaff.ManagerId,
                ManagerName = managerStaff.Manager?.User?.Fullname ?? "Unknown",
                ManagerEmail = managerStaff.Manager?.User?.Email,
                HireDate = managerStaff.HireDate.ToDateTime(TimeOnly.MinValue),
                AssignedPartners = assignedPartners,
                GrantedPermissions = grantedPermissions.OrderBy(p => p.PartnerName ?? "Global").ThenBy(p => p.ResourceType).ThenBy(p => p.ActionType).ToList()
            };
        }

        /// <summary>
        /// Helper method to get permissions for profile (simplified version)
        /// </summary>
        private async Task<ManagerStaffPermissionsListResponse> GetManagerStaffPermissionsForProfileAsync(int managerStaffId)
        {
            // Lấy tất cả permissions của ManagerStaff
            var permissions = await _context.ManagerStaffPartnerPermissions
                .Include(msp => msp.Permission)
                .Include(msp => msp.Partner)
                .Include(msp => msp.GrantedByUser)
                .Where(msp => msp.ManagerStaffId == managerStaffId && msp.IsActive)
                .ToListAsync();

            // Nhóm permissions theo Partner
            var partnerGroups = permissions
                .GroupBy(msp => msp.PartnerId)
                .Select(g => new PartnerPermissionsGroup
                {
                    PartnerId = g.Key ?? 0, // NULL = global permission
                    PartnerName = g.Key.HasValue 
                        ? (g.First().Partner?.PartnerName ?? "Unknown")
                        : "All Assigned Partners",
                    TaxCode = g.Key.HasValue ? g.First().Partner?.TaxCode : null,
                    Address = g.Key.HasValue ? g.First().Partner?.Address : null,
                    Permissions = g.Select(msp => new PermissionDetailResponse
                    {
                        PermissionId = msp.Permission.PermissionId,
                        PermissionCode = msp.Permission.PermissionCode,
                        PermissionName = msp.Permission.PermissionName,
                        ResourceType = msp.Permission.ResourceType,
                        ActionType = msp.Permission.ActionType,
                        Description = msp.Permission.Description,
                        GrantedAt = msp.GrantedAt,
                        GrantedByUserId = msp.GrantedBy,
                        GrantedByEmail = msp.GrantedByUser?.Email ?? "",
                        GrantedByName = msp.GrantedByUser?.Fullname,
                        IsActive = msp.IsActive
                    }).ToList()
                })
                .ToList();

            var managerStaff = await _context.ManagerStaffs
                .Include(ms => ms.User)
                .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId);

            return new ManagerStaffPermissionsListResponse
            {
                ManagerStaffId = managerStaffId,
                ManagerStaffName = managerStaff?.FullName ?? "Unknown",
                PartnerPermissions = partnerGroups
            };
        }
    }
}

