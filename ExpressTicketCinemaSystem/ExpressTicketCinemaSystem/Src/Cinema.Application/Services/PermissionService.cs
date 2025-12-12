using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly CinemaDbCoreContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(CinemaDbCoreContext context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int employeeId, int cinemaId, string permissionCode)
    {
        // First check if employee is assigned to this cinema
        var hasAssignment = await _context.EmployeeCinemaAssignments
            .AnyAsync(eca => eca.EmployeeId == employeeId && eca.CinemaId == cinemaId && eca.IsActive);

        if (!hasAssignment)
            return false; // Employee không được assign rạp này → không có quyền

        // Check permission: có thể là permission cụ thể cho cinema này hoặc global permission (null)
        var hasPermission = await _context.EmployeeCinemaPermissions
            .AnyAsync(ecp => ecp.EmployeeId == employeeId
                && (ecp.CinemaId == cinemaId || ecp.CinemaId == null)
                && ecp.Permission.PermissionCode == permissionCode
                && ecp.IsActive
                && ecp.Permission.IsActive);

        return hasPermission;
    }

    public async Task<bool> HasAnyPermissionAsync(int employeeId, int cinemaId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // First check if employee is assigned to this cinema
        var hasAssignment = await _context.EmployeeCinemaAssignments
            .AnyAsync(eca => eca.EmployeeId == employeeId && eca.CinemaId == cinemaId && eca.IsActive);

        if (!hasAssignment)
            return false; // Employee không được assign rạp này → không có quyền

        // Check permission: có thể là permission cụ thể cho cinema này hoặc global permission (null)
        var hasPermission = await _context.EmployeeCinemaPermissions
            .AnyAsync(ecp => ecp.EmployeeId == employeeId
                && (ecp.CinemaId == cinemaId || ecp.CinemaId == null)
                && permissionCodes.Contains(ecp.Permission.PermissionCode)
                && ecp.IsActive
                && ecp.Permission.IsActive);

        return hasPermission;
    }

    public async Task<bool> HasAllPermissionsAsync(int employeeId, int cinemaId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // First check if employee is assigned to this cinema
        var hasAssignment = await _context.EmployeeCinemaAssignments
            .AnyAsync(eca => eca.EmployeeId == employeeId && eca.CinemaId == cinemaId && eca.IsActive);

        if (!hasAssignment)
            return false; // Employee không được assign rạp này → không có quyền

        // Check permissions: có thể là permission cụ thể cho cinema này hoặc global permission (null)
        var grantedPermissions = await _context.EmployeeCinemaPermissions
            .Where(ecp => ecp.EmployeeId == employeeId
                && (ecp.CinemaId == cinemaId || ecp.CinemaId == null)
                && permissionCodes.Contains(ecp.Permission.PermissionCode)
                && ecp.IsActive
                && ecp.Permission.IsActive)
            .Select(ecp => ecp.Permission.PermissionCode)
            .Distinct()
            .ToListAsync();

        return permissionCodes.All(pc => grantedPermissions.Contains(pc));
    }

    public async Task<bool> HasAnyPermissionInAssignedCinemasAsync(int employeeId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // Lấy danh sách rạp được assign cho employee
        var assignedCinemaIds = await _context.EmployeeCinemaAssignments
            .Where(eca => eca.EmployeeId == employeeId && eca.IsActive)
            .Select(eca => eca.CinemaId)
            .ToListAsync();

        if (assignedCinemaIds.Count == 0)
            return false; // Employee chưa được assign rạp nào

        // Kiểm tra xem có quyền ở ít nhất 1 rạp được assign không
        foreach (var cinemaId in assignedCinemaIds)
        {
            if (await HasAnyPermissionAsync(employeeId, cinemaId, permissionCodes))
            {
                return true; // Có quyền ở ít nhất 1 rạp
            }
        }

        return false; // Không có quyền ở rạp nào
    }

    public async Task<PermissionActionResponse> GrantPermissionsAsync(int partnerId, int employeeId, GrantPermissionRequest request)
    {
        // Validate employee belongs to partner
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId && e.IsActive);

        if (employee == null)
            throw new NotFoundException("Không tìm thấy nhân viên hoặc nhân viên không thuộc quyền quản lý của bạn");

        // Lấy danh sách cinema được assign cho employee
        var assignedCinemaIds = await _context.EmployeeCinemaAssignments
            .Where(eca => eca.EmployeeId == employeeId && eca.IsActive)
            .Select(eca => eca.CinemaId)
            .ToListAsync();

        // Validate cinema assignment
        List<int>? validCinemaIds = null;

        if (request.CinemaIds != null && request.CinemaIds.Any())
        {
            // Validate từng cinemaId trong mảng
            var cinemas = await _context.Cinemas
                .Where(c => request.CinemaIds.Contains(c.CinemaId) && c.PartnerId == partnerId)
                .Select(c => c.CinemaId)
                .ToListAsync();

            // Chỉ lấy các cinemaIds đã được assign cho employee
            validCinemaIds = request.CinemaIds
                .Where(id => cinemas.Contains(id) && assignedCinemaIds.Contains(id))
                .Distinct()
                .ToList();

            if (validCinemaIds.Count == 0)
            {
                throw new ValidationException("cinemaIds", "Không có rạp nào hợp lệ hoặc nhân viên chưa được phân quyền các rạp này. Vui lòng phân quyền rạp cho nhân viên trước khi cấp quyền.", "cinemaIds");
            }

            // Nếu có cinemaIds không hợp lệ, báo cảnh báo (nhưng vẫn tiếp tục với các rạp hợp lệ)
            var invalidCinemaIds = request.CinemaIds.Except(validCinemaIds).ToList();
            if (invalidCinemaIds.Any())
            {
                _logger.LogWarning("Một số cinemaIds không hợp lệ hoặc chưa được assign: {InvalidIds}", string.Join(", ", invalidCinemaIds));
            }
        }
        else
        {
            // Khi CinemaIds == null hoặc rỗng (global permission), validate employee có ít nhất 1 cinema được assign
            if (!assignedCinemaIds.Any())
            {
                throw new ValidationException("cinemaIds", "Nhân viên chưa được phân quyền rạp nào. Vui lòng phân quyền ít nhất một rạp cho nhân viên trước khi cấp quyền global.", "cinemaIds");
            }
        }

        // Get permission IDs
        var permissions = await _context.Permissions
            .Where(p => request.PermissionCodes.Contains(p.PermissionCode) && p.IsActive)
            .ToListAsync();

        if (permissions.Count != request.PermissionCodes.Count)
        {
            var notFound = request.PermissionCodes.Except(permissions.Select(p => p.PermissionCode));
            throw new ValidationException("permissionCodes", $"Không tìm thấy các quyền: {string.Join(", ", notFound)}", "permissionCodes");
        }

        // Get User ID of Partner
        var partnerUser = await _context.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync();

        int grantedCount = 0;

        // Nếu có cinemaIds cụ thể, cấp quyền cho từng rạp
        if (validCinemaIds != null && validCinemaIds.Any())
        {
            foreach (var cinemaId in validCinemaIds)
            {
                foreach (var permission in permissions)
                {
                    // Check if permission already exists
                    var existing = await _context.EmployeeCinemaPermissions
                        .FirstOrDefaultAsync(ecp => ecp.EmployeeId == employeeId
                            && ecp.CinemaId == cinemaId
                            && ecp.PermissionId == permission.PermissionId);

                    if (existing != null)
                    {
                        // Re-activate if was revoked
                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            existing.GrantedBy = partnerUser;
                            existing.GrantedAt = DateTime.UtcNow;
                            existing.RevokedAt = null;
                            existing.RevokedBy = null;
                            grantedCount++;
                        }
                    }
                    else
                    {
                        // Create new permission
                        var newPermission = new EmployeeCinemaPermission
                        {
                            EmployeeId = employeeId,
                            CinemaId = cinemaId,
                            PermissionId = permission.PermissionId,
                            GrantedBy = partnerUser,
                            GrantedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.EmployeeCinemaPermissions.Add(newPermission);
                        grantedCount++;
                    }
                }
            }
        }
        else
        {
            // Cấp quyền global (CinemaId = null) cho tất cả rạp được assign
            foreach (var permission in permissions)
            {
                // Check if permission already exists
                var existing = await _context.EmployeeCinemaPermissions
                    .FirstOrDefaultAsync(ecp => ecp.EmployeeId == employeeId
                        && ecp.CinemaId == null
                        && ecp.PermissionId == permission.PermissionId);

                if (existing != null)
                {
                    // Re-activate if was revoked
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        existing.GrantedBy = partnerUser;
                        existing.GrantedAt = DateTime.UtcNow;
                        existing.RevokedAt = null;
                        existing.RevokedBy = null;
                        grantedCount++;
                    }
                }
                else
                {
                    // Create new global permission
                    var newPermission = new EmployeeCinemaPermission
                    {
                        EmployeeId = employeeId,
                        CinemaId = null, // Global permission
                        PermissionId = permission.PermissionId,
                        GrantedBy = partnerUser,
                        GrantedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.EmployeeCinemaPermissions.Add(newPermission);
                    grantedCount++;
                }
            }
        }

        await _context.SaveChangesAsync();

        // Tạo message chi tiết hơn
        string message;
        if (validCinemaIds != null && validCinemaIds.Any())
        {
            // Cấp quyền ở các rạp cụ thể
            var cinemaNames = await _context.Cinemas
                .Where(c => validCinemaIds.Contains(c.CinemaId))
                .Select(c => c.CinemaName)
                .ToListAsync();
            
            var permissionCount = request.PermissionCodes.Count;
            var cinemaCount = validCinemaIds.Count;
            
            message = $"Đã cấp {permissionCount} loại quyền cho {cinemaCount} rạp ({grantedCount} records): {string.Join(", ", cinemaNames)}";
        }
        else
        {
            // Cấp quyền global
            message = $"Đã cấp {grantedCount} quyền global (áp dụng cho tất cả rạp được assign)";
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = message,
            AffectedCount = grantedCount
        };
    }

    public async Task<PermissionActionResponse> RevokePermissionsAsync(int partnerId, int employeeId, RevokePermissionRequest request)
    {
        // Validate employee belongs to partner
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId && e.IsActive);

        if (employee == null)
            throw new NotFoundException("Không tìm thấy nhân viên hoặc nhân viên không thuộc quyền quản lý của bạn");

        // Lấy danh sách cinema được assign cho employee
        var assignedCinemaIds = await _context.EmployeeCinemaAssignments
            .Where(eca => eca.EmployeeId == employeeId && eca.IsActive)
            .Select(eca => eca.CinemaId)
            .ToListAsync();

        // Validate cinema assignment
        List<int>? validCinemaIds = null;

        if (request.CinemaIds != null && request.CinemaIds.Any())
        {
            // Validate từng cinemaId trong mảng
            var cinemas = await _context.Cinemas
                .Where(c => request.CinemaIds.Contains(c.CinemaId) && c.PartnerId == partnerId)
                .Select(c => c.CinemaId)
                .ToListAsync();

            // Chỉ lấy các cinemaIds đã được assign cho employee
            validCinemaIds = request.CinemaIds
                .Where(id => cinemas.Contains(id) && assignedCinemaIds.Contains(id))
                .Distinct()
                .ToList();

            if (validCinemaIds.Count == 0)
            {
                throw new ValidationException("cinemaIds", "Không có rạp nào hợp lệ hoặc nhân viên chưa được phân quyền các rạp này. Không thể thu hồi quyền ở rạp chưa được phân quyền.", "cinemaIds");
            }

            // Nếu có cinemaIds không hợp lệ, báo cảnh báo (nhưng vẫn tiếp tục với các rạp hợp lệ)
            var invalidCinemaIds = request.CinemaIds.Except(validCinemaIds).ToList();
            if (invalidCinemaIds.Any())
            {
                _logger.LogWarning("Một số cinemaIds không hợp lệ hoặc chưa được assign: {InvalidIds}", string.Join(", ", invalidCinemaIds));
            }
        }
        else
        {
            // Khi CinemaIds == null hoặc rỗng (global permission), validate employee có ít nhất 1 cinema được assign
            if (!assignedCinemaIds.Any())
            {
                throw new ValidationException("cinemaIds", "Nhân viên chưa được phân quyền rạp nào. Không thể thu hồi quyền global.", "cinemaIds");
            }
        }

        // Get permission IDs
        var permissionIds = await _context.Permissions
            .Where(p => request.PermissionCodes.Contains(p.PermissionCode) && p.IsActive)
            .Select(p => p.PermissionId)
            .ToListAsync();

        // Get User ID of Partner
        var partnerUser = await _context.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync();

        // Revoke permissions
        IQueryable<EmployeeCinemaPermission> revokeQuery = _context.EmployeeCinemaPermissions
            .Where(ecp => ecp.EmployeeId == employeeId
                && permissionIds.Contains(ecp.PermissionId)
                && ecp.IsActive);

        if (validCinemaIds != null && validCinemaIds.Any())
        {
            // Thu hồi quyền ở các rạp cụ thể
            revokeQuery = revokeQuery.Where(ecp => ecp.CinemaId.HasValue && validCinemaIds.Contains(ecp.CinemaId.Value));
        }
        else
        {
            // Thu hồi quyền global (CinemaId = null)
            revokeQuery = revokeQuery.Where(ecp => ecp.CinemaId == null);
        }

        var permissionsToRevoke = await revokeQuery.ToListAsync();

        foreach (var permission in permissionsToRevoke)
        {
            permission.IsActive = false;
            permission.RevokedAt = DateTime.UtcNow;
            permission.RevokedBy = partnerUser;
        }

        await _context.SaveChangesAsync();

        // Tạo message chi tiết hơn
        string message;
        if (validCinemaIds != null && validCinemaIds.Any())
        {
            // Thu hồi ở các rạp cụ thể
            var cinemaNames = await _context.Cinemas
                .Where(c => validCinemaIds.Contains(c.CinemaId))
                .Select(c => c.CinemaName)
                .ToListAsync();
            
            var permissionCount = request.PermissionCodes.Count;
            var cinemaCount = validCinemaIds.Count;
            var totalRecords = permissionsToRevoke.Count;
            
            message = $"Đã thu hồi {permissionCount} loại quyền ở {cinemaCount} rạp ({totalRecords} records): {string.Join(", ", cinemaNames)}";
        }
        else
        {
            // Thu hồi quyền global
            var permissionCount = permissionsToRevoke.Count;
            message = $"Đã thu hồi {permissionCount} quyền global (áp dụng cho tất cả rạp được assign)";
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = message,
            AffectedCount = permissionsToRevoke.Count
        };
    }

    public async Task<EmployeePermissionsListResponse> GetEmployeePermissionsAsync(int employeeId, List<int>? cinemaIds = null)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null)
            throw new NotFoundException("Không tìm thấy nhân viên");

        // Lấy danh sách cinema được assign cho employee với đầy đủ thông tin
        var assignedCinemas = await _context.EmployeeCinemaAssignments
            .Include(eca => eca.Cinema)
            .Where(eca => eca.EmployeeId == employeeId && eca.IsActive)
            .Select(eca => new
            {
                eca.CinemaId,
                eca.Cinema.CinemaName,
                eca.Cinema.Address,
                eca.Cinema.City,
                eca.Cinema.District
            })
            .ToListAsync();

        var assignedCinemaIds = assignedCinemas.Select(c => c.CinemaId).ToList();

        // Lọc theo cinemaIds nếu được cung cấp
        var targetCinemaIds = assignedCinemaIds;
        if (cinemaIds != null && cinemaIds.Any())
        {
            targetCinemaIds = cinemaIds.Where(id => assignedCinemaIds.Contains(id)).Distinct().ToList();
            if (targetCinemaIds.Count == 0)
            {
                // Không có cinema nào hợp lệ
                return new EmployeePermissionsListResponse
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee.FullName,
                    CinemaPermissions = new List<CinemaPermissionsGroup>()
                };
            }
        }

        // Lấy tất cả permissions (cả specific và global)
        var allPermissions = await _context.EmployeeCinemaPermissions
            .Include(ecp => ecp.Permission)
            .Include(ecp => ecp.GrantedByUser)
            .Where(ecp => ecp.EmployeeId == employeeId 
                && ecp.IsActive 
                && ecp.Permission.IsActive)
            .ToListAsync();

        // Phân loại permissions: specific và global
        var specificPermissions = allPermissions.Where(p => p.CinemaId.HasValue).ToList();
        var globalPermissions = allPermissions.Where(p => p.CinemaId == null).ToList();

        // Tạo danh sách permissions cho từng cinema
        var cinemaPermissionsGroups = new List<CinemaPermissionsGroup>();

        foreach (var cinema in assignedCinemas.Where(c => targetCinemaIds.Contains(c.CinemaId)))
        {
            var permissionsForThisCinema = new List<PermissionDetailResponse>();

            // Thêm các permissions cụ thể cho cinema này
            var specificPerms = specificPermissions
                .Where(p => p.CinemaId == cinema.CinemaId)
                .Select(p => new PermissionDetailResponse
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.Permission.PermissionCode,
                    PermissionName = p.Permission.PermissionName,
                    ResourceType = p.Permission.ResourceType,
                    ActionType = p.Permission.ActionType,
                    Description = p.Permission.Description,
                    GrantedAt = p.GrantedAt,
                    GrantedByUserId = p.GrantedBy,
                    GrantedByName = p.GrantedByUser?.Fullname ?? "Unknown",
                    GrantedByEmail = p.GrantedByUser?.Email,
                    IsActive = p.IsActive,
                    IsGlobalPermission = false
                })
                .ToList();

            permissionsForThisCinema.AddRange(specificPerms);

            // Thêm các global permissions (áp dụng cho tất cả cinemas được assign)
            // Chỉ thêm nếu chưa có permission cụ thể trùng PermissionCode
            var existingPermissionCodes = specificPerms.Select(p => p.PermissionCode).ToHashSet();
            
            var globalPerms = globalPermissions
                .Where(p => !existingPermissionCodes.Contains(p.Permission.PermissionCode))
                .Select(p => new PermissionDetailResponse
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.Permission.PermissionCode,
                    PermissionName = p.Permission.PermissionName,
                    ResourceType = p.Permission.ResourceType,
                    ActionType = p.Permission.ActionType,
                    Description = p.Permission.Description,
                    GrantedAt = p.GrantedAt,
                    GrantedByUserId = p.GrantedBy,
                    GrantedByName = p.GrantedByUser?.Fullname ?? "Unknown",
                    GrantedByEmail = p.GrantedByUser?.Email,
                    IsActive = p.IsActive,
                    IsGlobalPermission = true
                })
                .ToList();

            permissionsForThisCinema.AddRange(globalPerms);

            // Thêm nhóm cinema vào danh sách
            cinemaPermissionsGroups.Add(new CinemaPermissionsGroup
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                City = cinema.City,
                District = cinema.District,
                Permissions = permissionsForThisCinema.OrderBy(p => p.ResourceType).ThenBy(p => p.ActionType).ToList()
            });
        }

        return new EmployeePermissionsListResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            CinemaPermissions = cinemaPermissionsGroups.OrderBy(c => c.CinemaName).ToList()
        };
    }

    public async Task<AvailablePermissionsResponse> GetAvailablePermissionsAsync()
    {
        var permissions = await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.ResourceType)
            .ThenBy(p => p.ActionType)
            .ToListAsync();

        var grouped = permissions
            .GroupBy(p => p.ResourceType)
            .Select(g => new PermissionGroupResponse
            {
                ResourceType = g.Key,
                ResourceName = GetResourceDisplayName(g.Key),
                Permissions = g.Select(p => new PermissionResponse
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    ResourceType = p.ResourceType,
                    ActionType = p.ActionType,
                    Description = p.Description,
                    IsActive = p.IsActive
                }).ToList()
            })
            .ToList();

        return new AvailablePermissionsResponse
        {
            PermissionGroups = grouped
        };
    }

    public async Task<int?> GetEmployeeIdByUserIdAsync(int userId)
    {
        var employee = await _context.Employees
            .Where(e => e.UserId == userId && e.IsActive && e.RoleType == "Staff")
            .Select(e => e.EmployeeId)
            .FirstOrDefaultAsync();

        return employee > 0 ? employee : null;
    }

    private string GetResourceDisplayName(string resourceType)
    {
        return resourceType switch
        {
            "CINEMA" => "Quản lý rạp chiếu",
            "SCREEN" => "Quản lý phòng chiếu",
            "SEAT_TYPE" => "Quản lý loại ghế",
            "SEAT_LAYOUT" => "Quản lý sơ đồ ghế",
            "SHOWTIME" => "Quản lý suất chiếu",
            "SERVICE" => "Quản lý dịch vụ/combo",
            "BOOKING" => "Quản lý đặt vé",
            _ => resourceType
        };
    }
}

