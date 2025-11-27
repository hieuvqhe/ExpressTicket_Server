using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

/// <summary>
/// Filter để kiểm tra permission của Staff
/// Partner được bỏ qua (bypass) tất cả permission checks
/// </summary>
public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string[] _permissionCodes;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationFilter> _logger;

    public PermissionAuthorizationFilter(
        string[] permissionCodes,
        IPermissionService permissionService,
        ILogger<PermissionAuthorizationFilter> logger)
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

        // PARTNER có toàn quyền - bỏ qua permission check
        if (userRole == "Partner")
        {
            _logger.LogInformation("Partner user bypassing permission check");
            return;
        }

        // Chỉ Staff mới cần check permission
        if (userRole != "Staff")
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

        // Lấy Employee ID từ User ID
        var employeeId = await _permissionService.GetEmployeeIdByUserIdAsync(userId);
        if (!employeeId.HasValue)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Lấy Cinema ID từ route hoặc query string
        int? cinemaId = null;
        
        // Try to get from route values
        if (context.RouteData.Values.TryGetValue("cinema_id", out var cinemaIdObj))
        {
            if (int.TryParse(cinemaIdObj?.ToString(), out var cId))
                cinemaId = cId;
        }

        // If not in route, try query string
        if (!cinemaId.HasValue && context.HttpContext.Request.Query.TryGetValue("cinemaId", out var cinemaIdQuery))
        {
            if (int.TryParse(cinemaIdQuery.FirstOrDefault(), out var cId))
                cinemaId = cId;
        }

        // If still no cinema ID, try to extract from screen_id or showtime_id in route
        if (!cinemaId.HasValue)
        {
            cinemaId = await TryGetCinemaIdFromResourceAsync(context);
        }

        if (!cinemaId.HasValue)
        {
            context.Result = new BadRequestObjectResult(new ErrorResponse
            {
                Message = "Không xác định được rạp chiếu cần truy cập"
            });
            return;
        }

        // Check permission
        var hasPermission = await _permissionService.HasAnyPermissionAsync(
            employeeId.Value,
            cinemaId.Value,
            _permissionCodes);

        if (!hasPermission)
        {
            _logger.LogWarning("Staff user {UserId} (Employee {EmployeeId}) không có quyền {Permissions} cho Cinema {CinemaId}",
                userId, employeeId.Value, string.Join(", ", _permissionCodes), cinemaId.Value);

            context.Result = new ForbidResult();
            return;
        }

        _logger.LogInformation("Staff user {UserId} (Employee {EmployeeId}) có quyền {Permissions} cho Cinema {CinemaId}",
            userId, employeeId.Value, string.Join(", ", _permissionCodes), cinemaId.Value);
    }

    /// <summary>
    /// Cố gắng lấy Cinema ID từ các resource khác (screen, showtime, etc.)
    /// </summary>
    private async Task<int?> TryGetCinemaIdFromResourceAsync(AuthorizationFilterContext context)
    {
        // TODO: Implement logic để lấy cinema_id từ screen_id, showtime_id, etc.
        // Hiện tại return null, sẽ implement sau nếu cần
        return null;
    }
}




