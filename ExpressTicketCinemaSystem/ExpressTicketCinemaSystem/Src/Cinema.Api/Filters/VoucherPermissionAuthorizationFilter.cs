using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

/// <summary>
/// Filter để kiểm tra Voucher permission của ManagerStaff
/// Manager có toàn quyền và bỏ qua (bypass) tất cả permission checks
/// Voucher permissions are GLOBAL (không cần partnerId) - chỉ 1 ManagerStaff có thể có quyền tại 1 thời điểm
/// </summary>
public class VoucherPermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string[] _permissionCodes;
    private readonly IManagerStaffPermissionService _permissionService;
    private readonly ILogger<VoucherPermissionAuthorizationFilter> _logger;

    public VoucherPermissionAuthorizationFilter(
        string[] permissionCodes,
        IManagerStaffPermissionService permissionService,
        ILogger<VoucherPermissionAuthorizationFilter> logger)
    {
        _permissionCodes = permissionCodes;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Kiểm tra user đã authenticate chưa
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse
            {
                Message = "Bạn cần đăng nhập để truy cập tài nguyên này"
            });
            return;
        }

        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        // MANAGER có toàn quyền - bỏ qua permission check
        if (userRole == "Manager")
        {
            _logger.LogInformation("Manager user bypassing Voucher permission check");
            return;
        }

        // Chỉ ManagerStaff mới cần check permission
        if (userRole != "ManagerStaff")
        {
            context.Result = new ForbidResult();
            return;
        }

        // Lấy User ID
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.HttpContext.User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse
            {
                Message = "Token không hợp lệ"
            });
            return;
        }

        // Lấy ManagerStaff ID từ User ID
        var managerStaffId = await _permissionService.GetManagerStaffIdByUserIdAsync(userId);
        if (!managerStaffId.HasValue)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check Voucher permission (GLOBAL - không cần partnerId)
        // Chỉ cần check 1 trong các permissions được yêu cầu
        bool hasPermission = false;
        foreach (var permissionCode in _permissionCodes)
        {
            if (await _permissionService.HasVoucherPermissionAsync(managerStaffId.Value, permissionCode))
            {
                hasPermission = true;
                break;
            }
        }

        if (!hasPermission)
        {
            _logger.LogWarning("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) không có quyền {Permissions} cho Voucher",
                userId, managerStaffId.Value, string.Join(", ", _permissionCodes));

            context.Result = new ForbidResult();
            return;
        }

        _logger.LogInformation("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) có quyền {Permissions} cho Voucher",
            userId, managerStaffId.Value, string.Join(", ", _permissionCodes));
    }
}
