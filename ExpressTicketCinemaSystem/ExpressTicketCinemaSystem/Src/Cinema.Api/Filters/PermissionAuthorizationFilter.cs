using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        // Nếu không có cinemaId (GET ALL), kiểm tra Staff có quyền READ ở ít nhất 1 rạp được assign không
        if (!cinemaId.HasValue)
        {
            // Lấy danh sách rạp được assign cho Staff
            var hasAnyPermission = await _permissionService.HasAnyPermissionInAssignedCinemasAsync(
                employeeId.Value,
                _permissionCodes);

            if (!hasAnyPermission)
            {
                _logger.LogWarning("Staff user {UserId} (Employee {EmployeeId}) không có quyền {Permissions} ở bất kỳ rạp nào được assign",
                    userId, employeeId.Value, string.Join(", ", _permissionCodes));

                context.Result = new ForbidResult();
                return;
            }

            _logger.LogInformation("Staff user {UserId} (Employee {EmployeeId}) có quyền {Permissions} ở ít nhất 1 rạp được assign (GET ALL)",
                userId, employeeId.Value, string.Join(", ", _permissionCodes));
            return; // Cho phép GET ALL
        }

        // Nếu có cinemaId (GET BY ID), kiểm tra quyền ở rạp cụ thể
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
    private async Task<int?> TryGetCinemaIdFromResourceAsync(AuthorizationFilterContext filterContext)
    {
        // Try to get from screen_id or screenId in route
        int? screenId = null;
        if (filterContext.RouteData.Values.TryGetValue("screen_id", out var screenIdObj))
        {
            if (int.TryParse(screenIdObj?.ToString(), out var sid))
                screenId = sid;
        }
        else if (filterContext.RouteData.Values.TryGetValue("screenId", out var screenIdObj2))
        {
            if (int.TryParse(screenIdObj2?.ToString(), out var sid))
                screenId = sid;
        }

        if (screenId.HasValue)
        {
            // Query database để lấy cinemaId từ screenId
            using var scope = filterContext.HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();
            
            var cinemaId = await dbContext.Screens
                .Where(s => s.ScreenId == screenId.Value)
                .Select(s => s.CinemaId)
                .FirstOrDefaultAsync();
            
            if (cinemaId > 0)
                return cinemaId;
        }

        // Try to get from showtime_id or showtimeId in route
        int? showtimeId = null;
        if (filterContext.RouteData.Values.TryGetValue("showtime_id", out var showtimeIdObj))
        {
            if (int.TryParse(showtimeIdObj?.ToString(), out var stid))
                showtimeId = stid;
        }
        else if (filterContext.RouteData.Values.TryGetValue("showtimeId", out var showtimeIdObj2))
        {
            if (int.TryParse(showtimeIdObj2?.ToString(), out var stid))
                showtimeId = stid;
        }

        if (showtimeId.HasValue)
        {
            // Query database để lấy cinemaId từ showtimeId
            using var scope = filterContext.HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();
            
            var cinemaId = await dbContext.Showtimes
                .Where(s => s.ShowtimeId == showtimeId.Value)
                .Select(s => s.CinemaId)
                .FirstOrDefaultAsync();
            
            if (cinemaId > 0)
                return cinemaId;
        }

        return null;
    }
}




