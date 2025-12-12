using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public class ManagerStaffPermissionService : IManagerStaffPermissionService
{
    private readonly CinemaDbCoreContext _context;
    private readonly ILogger<ManagerStaffPermissionService> _logger;

    public ManagerStaffPermissionService(CinemaDbCoreContext context, ILogger<ManagerStaffPermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int managerStaffId, int partnerId, string permissionCode)
    {
        // First check if managerStaff is assigned to this partner
        var hasAssignment = await _context.Partners
            .AnyAsync(p => p.PartnerId == partnerId && p.ManagerStaffId == managerStaffId);

        if (!hasAssignment)
            return false; // ManagerStaff không được assign partner này → không có quyền

        // Check permission: có thể là permission cụ thể cho partner này hoặc global permission (null)
        var hasPermission = await _context.ManagerStaffPartnerPermissions
            .AnyAsync(msp => msp.ManagerStaffId == managerStaffId
                && (msp.PartnerId == partnerId || msp.PartnerId == null)
                && msp.Permission.PermissionCode == permissionCode
                && msp.IsActive
                && msp.Permission.IsActive);

        return hasPermission;
    }

    public async Task<bool> HasAnyPermissionAsync(int managerStaffId, int partnerId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // First check if managerStaff is assigned to this partner
        var hasAssignment = await _context.Partners
            .AnyAsync(p => p.PartnerId == partnerId && p.ManagerStaffId == managerStaffId);

        if (!hasAssignment)
            return false; // ManagerStaff không được assign partner này → không có quyền

        // Check permission: có thể là permission cụ thể cho partner này hoặc global permission (null)
        var hasPermission = await _context.ManagerStaffPartnerPermissions
            .AnyAsync(msp => msp.ManagerStaffId == managerStaffId
                && (msp.PartnerId == partnerId || msp.PartnerId == null)
                && permissionCodes.Contains(msp.Permission.PermissionCode)
                && msp.IsActive
                && msp.Permission.IsActive);

        return hasPermission;
    }

    public async Task<bool> HasAllPermissionsAsync(int managerStaffId, int partnerId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // First check if managerStaff is assigned to this partner
        var hasAssignment = await _context.Partners
            .AnyAsync(p => p.PartnerId == partnerId && p.ManagerStaffId == managerStaffId);

        if (!hasAssignment)
            return false; // ManagerStaff không được assign partner này → không có quyền

        // Check permissions: có thể là permission cụ thể cho partner này hoặc global permission (null)
        var grantedPermissions = await _context.ManagerStaffPartnerPermissions
            .Where(msp => msp.ManagerStaffId == managerStaffId
                && (msp.PartnerId == partnerId || msp.PartnerId == null)
                && permissionCodes.Contains(msp.Permission.PermissionCode)
                && msp.IsActive
                && msp.Permission.IsActive)
            .Select(msp => msp.Permission.PermissionCode)
            .Distinct()
            .ToListAsync();

        return permissionCodes.All(pc => grantedPermissions.Contains(pc));
    }

    public async Task<PermissionActionResponse> GrantPermissionsAsync(int managerId, int managerStaffId, GrantManagerStaffPermissionRequest request)
    {
        // Validate managerStaff belongs to manager
        var managerStaff = await _context.ManagerStaffs
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
            throw new NotFoundException("Không tìm thấy staff hoặc staff không thuộc quyền quản lý của bạn");

        // Lấy danh sách partners được assign cho managerStaff
        var assignedPartnerIds = await _context.Partners
            .Where(p => p.ManagerStaffId == managerStaffId)
            .Select(p => p.PartnerId)
            .ToListAsync();

        // Validate partner assignment
        List<int>? validPartnerIds = null;

        if (request.PartnerIds != null && request.PartnerIds.Any())
        {
            // Validate từng partnerId trong mảng
            var partners = await _context.Partners
                .Where(p => request.PartnerIds.Contains(p.PartnerId) && p.ManagerId == managerId)
                .Select(p => p.PartnerId)
                .ToListAsync();

            // Chỉ lấy các partnerIds đã được assign cho managerStaff
            validPartnerIds = request.PartnerIds
                .Where(id => partners.Contains(id) && assignedPartnerIds.Contains(id))
                .Distinct()
                .ToList();

            if (validPartnerIds.Count == 0)
            {
                throw new ValidationException("partnerIds", "Không có partner nào hợp lệ hoặc staff chưa được phân quyền các partner này. Vui lòng phân quyền partner cho staff trước khi cấp quyền.", "partnerIds");
            }

            // Nếu có partnerIds không hợp lệ, báo cảnh báo
            var invalidPartnerIds = request.PartnerIds.Except(validPartnerIds).ToList();
            if (invalidPartnerIds.Any())
            {
                _logger.LogWarning("Một số partnerIds không hợp lệ hoặc chưa được assign: {InvalidIds}", string.Join(", ", invalidPartnerIds));
            }
        }
        else
        {
            // Khi PartnerIds == null hoặc rỗng (global permission), validate managerStaff có ít nhất 1 partner được assign
            if (!assignedPartnerIds.Any())
            {
                throw new ValidationException("partnerIds", "Staff chưa được phân quyền partner nào. Vui lòng phân quyền ít nhất một partner cho staff trước khi cấp quyền global.", "partnerIds");
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

        // Get User ID of Manager
        var managerUser = await _context.Managers
            .Where(m => m.ManagerId == managerId)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync();

        int grantedCount = 0;

        // Nếu có partnerIds cụ thể, cấp quyền cho từng partner
        if (validPartnerIds != null && validPartnerIds.Any())
        {
            foreach (var partnerId in validPartnerIds)
            {
                foreach (var permission in permissions)
                {
                    // Check if permission already exists
                    var existing = await _context.ManagerStaffPartnerPermissions
                        .FirstOrDefaultAsync(msp => msp.ManagerStaffId == managerStaffId
                            && msp.PartnerId == partnerId
                            && msp.PermissionId == permission.PermissionId);

                    if (existing != null)
                    {
                        // Re-activate if was revoked
                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            existing.GrantedBy = managerUser;
                            existing.GrantedAt = DateTime.UtcNow;
                            existing.RevokedAt = null;
                            existing.RevokedBy = null;
                            grantedCount++;
                        }
                    }
                    else
                    {
                        // Create new permission
                        var newPermission = new ManagerStaffPartnerPermission
                        {
                            ManagerStaffId = managerStaffId,
                            PartnerId = partnerId,
                            PermissionId = permission.PermissionId,
                            GrantedBy = managerUser,
                            GrantedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.ManagerStaffPartnerPermissions.Add(newPermission);
                        grantedCount++;
                    }
                }
            }
        }
        else
        {
            // Cấp quyền global (PartnerId = null) cho tất cả partners được assign
            foreach (var permission in permissions)
            {
                // Check if permission already exists
                var existing = await _context.ManagerStaffPartnerPermissions
                    .FirstOrDefaultAsync(msp => msp.ManagerStaffId == managerStaffId
                        && msp.PartnerId == null
                        && msp.PermissionId == permission.PermissionId);

                if (existing != null)
                {
                    // Re-activate if was revoked
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        existing.GrantedBy = managerUser;
                        existing.GrantedAt = DateTime.UtcNow;
                        existing.RevokedAt = null;
                        existing.RevokedBy = null;
                        grantedCount++;
                    }
                }
                else
                {
                    // Create new global permission
                    var newPermission = new ManagerStaffPartnerPermission
                    {
                        ManagerStaffId = managerStaffId,
                        PartnerId = null, // Global permission
                        PermissionId = permission.PermissionId,
                        GrantedBy = managerUser,
                        GrantedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.ManagerStaffPartnerPermissions.Add(newPermission);
                    grantedCount++;
                }
            }
        }

        await _context.SaveChangesAsync();

        // Tạo message chi tiết hơn
        string message;
        if (validPartnerIds != null && validPartnerIds.Any())
        {
            // Cấp quyền ở các partner cụ thể
            var partnerNames = await _context.Partners
                .Where(p => validPartnerIds.Contains(p.PartnerId))
                .Select(p => p.PartnerName)
                .ToListAsync();
            
            var permissionCount = request.PermissionCodes.Count;
            var partnerCount = validPartnerIds.Count;
            
            message = $"Đã cấp {permissionCount} loại quyền cho {partnerCount} partner ({grantedCount} records): {string.Join(", ", partnerNames)}";
        }
        else
        {
            // Cấp quyền global
            message = $"Đã cấp {grantedCount} quyền global (áp dụng cho tất cả partners được assign)";
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = message,
            AffectedCount = grantedCount
        };
    }

    public async Task<PermissionActionResponse> RevokePermissionsAsync(int managerId, int managerStaffId, RevokeManagerStaffPermissionRequest request)
    {
        // Validate managerStaff belongs to manager
        var managerStaff = await _context.ManagerStaffs
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
            throw new NotFoundException("Không tìm thấy staff hoặc staff không thuộc quyền quản lý của bạn");

        // Lấy danh sách partners được assign cho managerStaff
        var assignedPartnerIds = await _context.Partners
            .Where(p => p.ManagerStaffId == managerStaffId)
            .Select(p => p.PartnerId)
            .ToListAsync();

        // Validate partner assignment
        List<int>? validPartnerIds = null;

        if (request.PartnerIds != null && request.PartnerIds.Any())
        {
            // Validate từng partnerId trong mảng
            var partners = await _context.Partners
                .Where(p => request.PartnerIds.Contains(p.PartnerId) && p.ManagerId == managerId)
                .Select(p => p.PartnerId)
                .ToListAsync();

            // Chỉ lấy các partnerIds đã được assign cho managerStaff
            validPartnerIds = request.PartnerIds
                .Where(id => partners.Contains(id) && assignedPartnerIds.Contains(id))
                .Distinct()
                .ToList();

            if (validPartnerIds.Count == 0)
            {
                throw new ValidationException("partnerIds", "Không có partner nào hợp lệ hoặc staff chưa được phân quyền các partner này. Không thể thu hồi quyền ở partner chưa được phân quyền.", "partnerIds");
            }

            // Nếu có partnerIds không hợp lệ, báo cảnh báo
            var invalidPartnerIds = request.PartnerIds.Except(validPartnerIds).ToList();
            if (invalidPartnerIds.Any())
            {
                _logger.LogWarning("Một số partnerIds không hợp lệ hoặc chưa được assign: {InvalidIds}", string.Join(", ", invalidPartnerIds));
            }
        }
        else
        {
            // Khi PartnerIds == null hoặc rỗng (global permission), validate managerStaff có ít nhất 1 partner được assign
            if (!assignedPartnerIds.Any())
            {
                throw new ValidationException("partnerIds", "Staff chưa được phân quyền partner nào. Không thể thu hồi quyền global.", "partnerIds");
            }
        }

        // Get permission IDs
        var permissionIds = await _context.Permissions
            .Where(p => request.PermissionCodes.Contains(p.PermissionCode) && p.IsActive)
            .Select(p => p.PermissionId)
            .ToListAsync();

        // Get User ID of Manager
        var managerUser = await _context.Managers
            .Where(m => m.ManagerId == managerId)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync();

        // Revoke permissions
        IQueryable<ManagerStaffPartnerPermission> revokeQuery = _context.ManagerStaffPartnerPermissions
            .Where(msp => msp.ManagerStaffId == managerStaffId
                && permissionIds.Contains(msp.PermissionId)
                && msp.IsActive);

        if (validPartnerIds != null && validPartnerIds.Any())
        {
            // Thu hồi quyền ở các partner cụ thể
            revokeQuery = revokeQuery.Where(msp => msp.PartnerId.HasValue && validPartnerIds.Contains(msp.PartnerId.Value));
        }
        else
        {
            // Thu hồi quyền global (PartnerId = null)
            revokeQuery = revokeQuery.Where(msp => msp.PartnerId == null);
        }

        var permissionsToRevoke = await revokeQuery.ToListAsync();

        foreach (var permission in permissionsToRevoke)
        {
            permission.IsActive = false;
            permission.RevokedAt = DateTime.UtcNow;
            permission.RevokedBy = managerUser;
        }

        await _context.SaveChangesAsync();

        // Tạo message chi tiết hơn
        string message;
        if (validPartnerIds != null && validPartnerIds.Any())
        {
            // Thu hồi ở các partner cụ thể
            var partnerNames = await _context.Partners
                .Where(p => validPartnerIds.Contains(p.PartnerId))
                .Select(p => p.PartnerName)
                .ToListAsync();
            
            var permissionCount = request.PermissionCodes.Count;
            var partnerCount = validPartnerIds.Count;
            var totalRecords = permissionsToRevoke.Count;
            
            message = $"Đã thu hồi {permissionCount} loại quyền ở {partnerCount} partner ({totalRecords} records): {string.Join(", ", partnerNames)}";
        }
        else
        {
            // Thu hồi quyền global
            var permissionCount = permissionsToRevoke.Count;
            message = $"Đã thu hồi {permissionCount} quyền global (áp dụng cho tất cả partners được assign)";
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = message,
            AffectedCount = permissionsToRevoke.Count
        };
    }

    public async Task<ManagerStaffPermissionsListResponse> GetManagerStaffPermissionsAsync(int managerStaffId, List<int>? partnerIds = null)
    {
        var managerStaff = await _context.ManagerStaffs
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId);

        if (managerStaff == null)
            throw new NotFoundException("Không tìm thấy ManagerStaff");

        // Lấy danh sách partners được assign cho managerStaff với đầy đủ thông tin
        var assignedPartners = await _context.Partners
            .Where(p => p.ManagerStaffId == managerStaffId)
            .Select(p => new
            {
                p.PartnerId,
                p.PartnerName,
                p.TaxCode,
                p.Address
            })
            .ToListAsync();

        var assignedPartnerIds = assignedPartners.Select(p => p.PartnerId).ToList();

        // Lọc theo partnerIds nếu được cung cấp
        var targetPartnerIds = assignedPartnerIds;
        if (partnerIds != null && partnerIds.Any())
        {
            targetPartnerIds = partnerIds.Where(id => assignedPartnerIds.Contains(id)).Distinct().ToList();
            if (targetPartnerIds.Count == 0)
            {
                // Không có partner nào hợp lệ
                return new ManagerStaffPermissionsListResponse
                {
                    ManagerStaffId = managerStaffId,
                    ManagerStaffName = managerStaff.FullName,
                    PartnerPermissions = new List<PartnerPermissionsGroup>()
                };
            }
        }

        // Lấy tất cả permissions (cả specific và global)
        var allPermissions = await _context.ManagerStaffPartnerPermissions
            .Include(msp => msp.Permission)
            .Include(msp => msp.GrantedByUser)
            .Where(msp => msp.ManagerStaffId == managerStaffId 
                && msp.IsActive 
                && msp.Permission.IsActive)
            .ToListAsync();

        // Phân loại permissions: specific và global
        var specificPermissions = allPermissions.Where(p => p.PartnerId.HasValue).ToList();
        var globalPermissions = allPermissions.Where(p => p.PartnerId == null).ToList();

        // Tạo danh sách permissions cho từng partner
        var partnerPermissionsGroups = new List<PartnerPermissionsGroup>();

        foreach (var partner in assignedPartners.Where(p => targetPartnerIds.Contains(p.PartnerId)))
        {
            var permissionsForThisPartner = new List<PermissionDetailResponse>();

            // Thêm các permissions cụ thể cho partner này
            var specificPerms = specificPermissions
                .Where(p => p.PartnerId == partner.PartnerId)
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

            permissionsForThisPartner.AddRange(specificPerms);

            // Thêm các global permissions (áp dụng cho tất cả partners được assign)
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

            permissionsForThisPartner.AddRange(globalPerms);

            // Thêm nhóm partner vào danh sách
            partnerPermissionsGroups.Add(new PartnerPermissionsGroup
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Address = partner.Address,
                Permissions = permissionsForThisPartner.OrderBy(p => p.ResourceType).ThenBy(p => p.ActionType).ToList()
            });
        }

        return new ManagerStaffPermissionsListResponse
        {
            ManagerStaffId = managerStaffId,
            ManagerStaffName = managerStaff.FullName,
            PartnerPermissions = partnerPermissionsGroups.OrderBy(p => p.PartnerName).ToList()
        };
    }

    public async Task<AvailablePermissionsResponse> GetAvailablePermissionsAsync()
    {
        // Lấy tất cả permissions, nhưng filter theo resource types phù hợp với ManagerStaff
        // NOTE: REPORT and BOOKING are NOT included - these APIs are accessible by default
        // The difference is in data scope (Manager sees all, ManagerStaff sees only assigned partners)
        var managerStaffResourceTypes = new[] { "PARTNER", "CONTRACT", "MOVIE_SUBMISSION", "VOUCHER" };
        
        var permissions = await _context.Permissions
            .Where(p => p.IsActive && managerStaffResourceTypes.Contains(p.ResourceType))
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

    public async Task<int?> GetManagerStaffIdByUserIdAsync(int userId)
    {
        var managerStaff = await _context.ManagerStaffs
            .Where(ms => ms.UserId == userId && ms.IsActive)
            .Select(ms => ms.ManagerStaffId)
            .FirstOrDefaultAsync();

        return managerStaff > 0 ? managerStaff : null;
    }

    public async Task<bool> HasAnyPermissionInAssignedPartnersAsync(int managerStaffId, params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
            return false;

        // Lấy danh sách partners được assign cho managerStaff
        var assignedPartnerIds = await _context.Partners
            .Where(p => p.ManagerStaffId == managerStaffId)
            .Select(p => p.PartnerId)
            .ToListAsync();

        if (assignedPartnerIds.Count == 0)
            return false; // ManagerStaff chưa được assign partner nào

        // Kiểm tra xem có quyền ở ít nhất 1 partner được assign không
        foreach (var partnerId in assignedPartnerIds)
        {
            if (await HasAnyPermissionAsync(managerStaffId, partnerId, permissionCodes))
            {
                return true; // Có quyền ở ít nhất 1 partner
            }
        }

        return false; // Không có quyền ở partner nào
    }

    private string GetResourceDisplayName(string resourceType)
    {
        return resourceType switch
        {
            "PARTNER" => "Quản lý đối tác",
            "CONTRACT" => "Quản lý hợp đồng",
            "MOVIE_SUBMISSION" => "Quản lý duyệt phim",
            "VOUCHER" => "Quản lý voucher",
            "REPORT" => "Xem báo cáo",
            "BOOKING" => "Quản lý đặt vé",
            _ => resourceType
        };
    }

    /// <summary>
    /// Get list of valid permission codes for ManagerStaff
    /// This ensures only permissions with corresponding APIs are available
    /// NOTE: REPORT and BOOKING APIs don't require permissions - they're accessible by default
    /// The difference is in data scope (Manager sees all, ManagerStaff sees only assigned partners)
    /// </summary>
    public static List<string> GetValidManagerStaffPermissions()
    {
        return new List<string>
        {
            // PARTNER Permissions
            "PARTNER_READ",
            "PARTNER_APPROVE",
            "PARTNER_REJECT",
            
            // CONTRACT Permissions
            "CONTRACT_CREATE",
            "CONTRACT_READ",
            "CONTRACT_UPDATE",
            "CONTRACT_SIGN_TEMPORARY",
            "CONTRACT_FINALIZE", // Only Manager can use, but permission exists
            "CONTRACT_SEND_PDF",
            
            // MOVIE_SUBMISSION Permissions
            "MOVIE_SUBMISSION_READ",
            "MOVIE_SUBMISSION_APPROVE",
            "MOVIE_SUBMISSION_REJECT",
            
            // VOUCHER Permissions (GLOBAL - no partnerId required)
            "VOUCHER_CREATE",
            "VOUCHER_READ",
            "VOUCHER_UPDATE",
            "VOUCHER_DELETE",
            "VOUCHER_SEND"
            
            // NOTE: REPORT and BOOKING permissions are NOT included
            // These APIs are accessible to both Manager and ManagerStaff by default
            // Data filtering is handled in the service layer based on user role
        };
    }

    /// <summary>
    /// Kiểm tra ManagerStaff có Voucher permission không (GLOBAL - không cần partnerId)
    /// </summary>
    public async Task<bool> HasVoucherPermissionAsync(int managerStaffId, string permissionCode)
    {
        // Check if ManagerStaff has this Voucher permission (global - PartnerId = null)
        var hasPermission = await _context.ManagerStaffPartnerPermissions
            .AnyAsync(msp => msp.ManagerStaffId == managerStaffId
                && msp.PartnerId == null // GLOBAL permission
                && msp.Permission.PermissionCode == permissionCode
                && msp.Permission.ResourceType == "VOUCHER"
                && msp.IsActive
                && msp.Permission.IsActive);

        return hasPermission;
    }

    /// <summary>
    /// Cấp Voucher permission cho ManagerStaff (GLOBAL - không cần partnerId)
    /// Tự động revoke permission từ ManagerStaff khác nếu có
    /// </summary>
    public async Task<PermissionActionResponse> GrantVoucherPermissionAsync(int managerId, int managerStaffId, string permissionCode)
    {
        // Validate ManagerStaff belongs to Manager
        var managerStaff = await _context.ManagerStaffs
            .Include(ms => ms.User)
            .Include(ms => ms.Manager)
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
        {
            throw new NotFoundException("Không tìm thấy ManagerStaff hoặc ManagerStaff không thuộc quyền quản lý của bạn.");
        }

        // Validate permission exists and is VOUCHER type
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionCode == permissionCode 
                && p.ResourceType == "VOUCHER" 
                && p.IsActive);

        if (permission == null)
        {
            throw new NotFoundException($"Không tìm thấy permission {permissionCode} hoặc permission không hợp lệ.");
        }

        // QUAN TRỌNG: Validate - Mỗi permission chỉ có thể được cấp cho 1 ManagerStaff duy nhất
        // Kiểm tra xem có ManagerStaff khác đã có permission này chưa
        var otherStaffPermission = await _context.ManagerStaffPartnerPermissions
            .Include(msp => msp.ManagerStaff)
            .ThenInclude(ms => ms.User)
            .FirstOrDefaultAsync(msp => msp.PartnerId == null
                && msp.Permission.ResourceType == "VOUCHER"
                && msp.Permission.PermissionCode == permissionCode
                && msp.IsActive
                && msp.ManagerStaffId != managerStaffId);

        if (otherStaffPermission != null)
        {
            var otherStaffName = otherStaffPermission.ManagerStaff.FullName;
            throw new ConflictException("permission", 
                $"Không thể cấp quyền {permissionCode}. Quyền này đã được cấp cho ManagerStaff '{otherStaffName}' (ID: {otherStaffPermission.ManagerStaffId}). " +
                $"Vui lòng thu hồi quyền từ ManagerStaff đó trước khi cấp cho ManagerStaff khác.");
        }

        // Check if ManagerStaff already has this permission
        var existingPermission = await _context.ManagerStaffPartnerPermissions
            .FirstOrDefaultAsync(msp => msp.ManagerStaffId == managerStaffId
                && msp.PartnerId == null
                && msp.PermissionId == permission.PermissionId
                && msp.Permission.ResourceType == "VOUCHER");

        if (existingPermission != null)
        {
            if (existingPermission.IsActive)
            {
                throw new ConflictException("permission", $"ManagerStaff đã có quyền {permissionCode}.");
            }
            else
            {
                // Reactivate existing permission
                existingPermission.IsActive = true;
                existingPermission.RevokedAt = null;
                existingPermission.RevokedBy = null;
                existingPermission.GrantedAt = DateTime.UtcNow;
                existingPermission.GrantedBy = managerId;
            }
        }
        else
        {
            // Create new permission
            var newPermission = new ManagerStaffPartnerPermission
            {
                ManagerStaffId = managerStaffId,
                PartnerId = null, // GLOBAL permission - không cần partnerId
                PermissionId = permission.PermissionId,
                GrantedBy = managerId,
                GrantedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.ManagerStaffPartnerPermissions.Add(newPermission);
        }

        await _context.SaveChangesAsync();

        return new PermissionActionResponse
        {
            Success = true,
            Message = $"Đã cấp quyền {permission.PermissionName} cho ManagerStaff {managerStaff.FullName} thành công."
        };
    }

    /// <summary>
    /// Thu hồi Voucher permission từ ManagerStaff (GLOBAL - không cần partnerId)
    /// </summary>
    public async Task<PermissionActionResponse> RevokeVoucherPermissionAsync(int managerId, int managerStaffId, string permissionCode)
    {
        // Validate ManagerStaff belongs to Manager
        var managerStaff = await _context.ManagerStaffs
            .Include(ms => ms.User)
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
        {
            throw new NotFoundException("Không tìm thấy ManagerStaff hoặc ManagerStaff không thuộc quyền quản lý của bạn.");
        }

        // Find the permission
        var permission = await _context.ManagerStaffPartnerPermissions
            .Include(msp => msp.Permission)
            .FirstOrDefaultAsync(msp => msp.ManagerStaffId == managerStaffId
                && msp.PartnerId == null // GLOBAL permission
                && msp.Permission.PermissionCode == permissionCode
                && msp.Permission.ResourceType == "VOUCHER"
                && msp.IsActive);

        if (permission == null)
        {
            throw new NotFoundException($"ManagerStaff không có quyền {permissionCode}.");
        }

        // Revoke permission
        permission.IsActive = false;
        permission.RevokedAt = DateTime.UtcNow;
        permission.RevokedBy = managerId;

        await _context.SaveChangesAsync();

        return new PermissionActionResponse
        {
            Success = true,
            Message = $"Đã thu hồi quyền {permission.Permission.PermissionName} từ ManagerStaff {managerStaff.FullName} thành công."
        };
    }

    /// <summary>
    /// Lấy ManagerStaff ID hiện có Voucher permission (nếu có)
    /// </summary>
    public async Task<int?> GetManagerStaffIdWithVoucherPermissionAsync()
    {
        // Lấy ManagerStaff ID có bất kỳ Voucher permission nào (active)
        var managerStaffId = await _context.ManagerStaffPartnerPermissions
            .Where(msp => msp.PartnerId == null
                && msp.Permission.ResourceType == "VOUCHER"
                && msp.IsActive
                && msp.Permission.IsActive)
            .Select(msp => msp.ManagerStaffId)
            .FirstOrDefaultAsync();

        return managerStaffId > 0 ? managerStaffId : null;
    }

    /// <summary>
    /// Cấp nhiều Voucher permissions cho ManagerStaff cùng lúc (GLOBAL - không cần partnerId)
    /// Tự động revoke permissions từ ManagerStaff khác nếu có
    /// Validate: Mỗi permission chỉ có thể được cấp cho 1 ManagerStaff duy nhất
    /// </summary>
    public async Task<PermissionActionResponse> GrantMultipleVoucherPermissionsAsync(int managerId, int managerStaffId, List<string> permissionCodes)
    {
        if (permissionCodes == null || !permissionCodes.Any())
        {
            throw new ValidationException("PermissionCodes", "Danh sách quyền không được để trống");
        }

        // Validate ManagerStaff belongs to Manager
        var managerStaff = await _context.ManagerStaffs
            .Include(ms => ms.User)
            .Include(ms => ms.Manager)
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
        {
            throw new NotFoundException("Không tìm thấy ManagerStaff hoặc ManagerStaff không thuộc quyền quản lý của bạn.");
        }

        // Validate all permissions exist and are VOUCHER type
        var permissions = await _context.Permissions
            .Where(p => permissionCodes.Contains(p.PermissionCode) 
                && p.ResourceType == "VOUCHER" 
                && p.IsActive)
            .ToListAsync();

        if (permissions.Count != permissionCodes.Count)
        {
            var foundCodes = permissions.Select(p => p.PermissionCode).ToList();
            var missingCodes = permissionCodes.Except(foundCodes).ToList();
            throw new NotFoundException($"Không tìm thấy các quyền sau: {string.Join(", ", missingCodes)}");
        }

        // QUAN TRỌNG: Validate mỗi permission chỉ có thể được cấp cho 1 ManagerStaff duy nhất
        // Kiểm tra xem có ManagerStaff khác đã có các permissions này chưa
        var existingPermissions = await _context.ManagerStaffPartnerPermissions
            .Include(msp => msp.Permission)
            .Where(msp => msp.PartnerId == null
                && msp.Permission.ResourceType == "VOUCHER"
                && permissionCodes.Contains(msp.Permission.PermissionCode)
                && msp.IsActive
                && msp.ManagerStaffId != managerStaffId)
            .ToListAsync();

        if (existingPermissions.Any())
        {
            var conflictInfo = existingPermissions
                .GroupBy(ep => ep.ManagerStaffId)
                .Select(g => new
                {
                    ManagerStaffId = g.Key,
                    Permissions = g.Select(ep => ep.Permission.PermissionCode).ToList()
                })
                .ToList();

            var conflictMessages = conflictInfo.Select(ci => 
                $"ManagerStaff ID {ci.ManagerStaffId} đã có các quyền: {string.Join(", ", ci.Permissions)}");

            throw new ConflictException("permission", 
                $"Không thể cấp quyền. Các quyền sau đã được cấp cho ManagerStaff khác:\n{string.Join("\n", conflictMessages)}");
        }

        // Note: Không tự động revoke permissions từ ManagerStaff khác
        // Validation đã được thực hiện ở trên - nếu có conflict thì đã throw exception

        var grantedPermissions = new List<string>();
        var reactivatedPermissions = new List<string>();

        // Grant each permission
        foreach (var permission in permissions)
        {
            // Check if ManagerStaff already has this permission
            var existingPermission = await _context.ManagerStaffPartnerPermissions
                .FirstOrDefaultAsync(msp => msp.ManagerStaffId == managerStaffId
                    && msp.PartnerId == null
                    && msp.PermissionId == permission.PermissionId
                    && msp.Permission.ResourceType == "VOUCHER");

            if (existingPermission != null)
            {
                if (existingPermission.IsActive)
                {
                    // Already has this permission - skip
                    continue;
                }
                else
                {
                    // Reactivate existing permission
                    existingPermission.IsActive = true;
                    existingPermission.RevokedAt = null;
                    existingPermission.RevokedBy = null;
                    existingPermission.GrantedAt = DateTime.UtcNow;
                    existingPermission.GrantedBy = managerId;
                    reactivatedPermissions.Add(permission.PermissionCode);
                }
            }
            else
            {
                // Create new permission
                var newPermission = new ManagerStaffPartnerPermission
                {
                    ManagerStaffId = managerStaffId,
                    PartnerId = null, // GLOBAL permission - không cần partnerId
                    PermissionId = permission.PermissionId,
                    GrantedBy = managerId,
                    GrantedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.ManagerStaffPartnerPermissions.Add(newPermission);
                grantedPermissions.Add(permission.PermissionCode);
            }
        }

        await _context.SaveChangesAsync();

        var messageParts = new List<string>();
        if (grantedPermissions.Any())
        {
            messageParts.Add($"Đã cấp mới {grantedPermissions.Count} quyền: {string.Join(", ", grantedPermissions)}");
        }
        if (reactivatedPermissions.Any())
        {
            messageParts.Add($"Đã kích hoạt lại {reactivatedPermissions.Count} quyền: {string.Join(", ", reactivatedPermissions)}");
        }
        if (grantedPermissions.Count + reactivatedPermissions.Count < permissionCodes.Count)
        {
            var skipped = permissionCodes.Except(grantedPermissions).Except(reactivatedPermissions).ToList();
            messageParts.Add($"{skipped.Count} quyền đã có sẵn: {string.Join(", ", skipped)}");
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = $"Đã cấp quyền Voucher cho ManagerStaff {managerStaff.FullName} thành công. {string.Join(". ", messageParts)}."
        };
    }

    /// <summary>
    /// Thu hồi nhiều Voucher permissions từ ManagerStaff cùng lúc (GLOBAL - không cần partnerId)
    /// </summary>
    public async Task<PermissionActionResponse> RevokeMultipleVoucherPermissionsAsync(int managerId, int managerStaffId, List<string> permissionCodes)
    {
        if (permissionCodes == null || !permissionCodes.Any())
        {
            throw new ValidationException("PermissionCodes", "Danh sách quyền không được để trống");
        }

        // Validate ManagerStaff belongs to Manager
        var managerStaff = await _context.ManagerStaffs
            .Include(ms => ms.User)
            .Include(ms => ms.Manager)
            .FirstOrDefaultAsync(ms => ms.ManagerStaffId == managerStaffId && ms.ManagerId == managerId && ms.IsActive);

        if (managerStaff == null)
        {
            throw new NotFoundException("Không tìm thấy ManagerStaff hoặc ManagerStaff không thuộc quyền quản lý của bạn.");
        }

        // Validate all permissions exist and are VOUCHER type
        var permissions = await _context.Permissions
            .Where(p => permissionCodes.Contains(p.PermissionCode) 
                && p.ResourceType == "VOUCHER" 
                && p.IsActive)
            .ToListAsync();

        if (permissions.Count != permissionCodes.Count)
        {
            var foundCodes = permissions.Select(p => p.PermissionCode).ToList();
            var missingCodes = permissionCodes.Except(foundCodes).ToList();
            throw new NotFoundException($"Không tìm thấy các quyền sau: {string.Join(", ", missingCodes)}");
        }

        // Find all active permissions to revoke
        var permissionsToRevoke = await _context.ManagerStaffPartnerPermissions
            .Include(msp => msp.Permission)
            .Where(msp => msp.ManagerStaffId == managerStaffId
                && msp.PartnerId == null // GLOBAL permission
                && permissionCodes.Contains(msp.Permission.PermissionCode)
                && msp.Permission.ResourceType == "VOUCHER"
                && msp.IsActive)
            .ToListAsync();

        if (!permissionsToRevoke.Any())
        {
            throw new NotFoundException($"ManagerStaff không có bất kỳ quyền nào trong danh sách: {string.Join(", ", permissionCodes)}");
        }

        var revokedPermissions = new List<string>();
        var notFoundPermissions = new List<string>();

        // Revoke each permission
        foreach (var permissionCode in permissionCodes)
        {
            var permissionToRevoke = permissionsToRevoke
                .FirstOrDefault(msp => msp.Permission.PermissionCode == permissionCode);

            if (permissionToRevoke != null)
            {
                permissionToRevoke.IsActive = false;
                permissionToRevoke.RevokedAt = DateTime.UtcNow;
                permissionToRevoke.RevokedBy = managerId;
                revokedPermissions.Add(permissionCode);
            }
            else
            {
                notFoundPermissions.Add(permissionCode);
            }
        }

        await _context.SaveChangesAsync();

        var messageParts = new List<string>();
        if (revokedPermissions.Any())
        {
            messageParts.Add($"Đã thu hồi {revokedPermissions.Count} quyền: {string.Join(", ", revokedPermissions)}");
        }
        if (notFoundPermissions.Any())
        {
            messageParts.Add($"{notFoundPermissions.Count} quyền không tồn tại hoặc đã bị thu hồi: {string.Join(", ", notFoundPermissions)}");
        }

        return new PermissionActionResponse
        {
            Success = true,
            Message = $"Đã thu hồi quyền Voucher từ ManagerStaff {managerStaff.FullName}. {string.Join(". ", messageParts)}."
        };
    }
}

