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
/// Filter để kiểm tra permission của ManagerStaff
/// Manager có toàn quyền và bỏ qua (bypass) tất cả permission checks
/// </summary>
public class ManagerStaffPermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly string[] _permissionCodes;
    private readonly IManagerStaffPermissionService _permissionService;
    private readonly ILogger<ManagerStaffPermissionAuthorizationFilter> _logger;

    public ManagerStaffPermissionAuthorizationFilter(
        string[] permissionCodes,
        IManagerStaffPermissionService permissionService,
        ILogger<ManagerStaffPermissionAuthorizationFilter> logger)
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
            _logger.LogInformation("Manager user bypassing permission check");
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

        // Lấy Partner ID từ route, query string, hoặc request body
        int? partnerId = await TryGetPartnerIdAsync(context);

        // Nếu không có partnerId (GET ALL), kiểm tra ManagerStaff có quyền ở ít nhất 1 partner được assign không
        if (!partnerId.HasValue)
        {
            // Lấy danh sách partners được assign cho ManagerStaff
            var hasAnyPermission = await _permissionService.HasAnyPermissionInAssignedPartnersAsync(
                managerStaffId.Value,
                _permissionCodes);

            if (!hasAnyPermission)
            {
                _logger.LogWarning("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) không có quyền {Permissions} ở bất kỳ partner nào được assign",
                    userId, managerStaffId.Value, string.Join(", ", _permissionCodes));

                context.Result = new ForbidResult();
                return;
            }

            _logger.LogInformation("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) có quyền {Permissions} ở ít nhất 1 partner được assign (GET ALL)",
                userId, managerStaffId.Value, string.Join(", ", _permissionCodes));
            return; // Cho phép GET ALL
        }

        // Nếu có partnerId (GET BY ID hoặc specific action), kiểm tra quyền ở partner cụ thể
        var hasPermission = await _permissionService.HasAnyPermissionAsync(
            managerStaffId.Value,
            partnerId.Value,
            _permissionCodes);

        if (!hasPermission)
        {
            _logger.LogWarning("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) không có quyền {Permissions} cho Partner {PartnerId}",
                userId, managerStaffId.Value, string.Join(", ", _permissionCodes), partnerId.Value);

            context.Result = new ForbidResult();
            return;
        }

        _logger.LogInformation("ManagerStaff user {UserId} (ManagerStaff {ManagerStaffId}) có quyền {Permissions} cho Partner {PartnerId}",
            userId, managerStaffId.Value, string.Join(", ", _permissionCodes), partnerId.Value);
    }

    /// <summary>
    /// Cố gắng lấy Partner ID từ route, query string, request body, hoặc contract
    /// </summary>
    private async Task<int?> TryGetPartnerIdAsync(AuthorizationFilterContext filterContext)
    {
        // Try to get from route values (partner_id, partnerId, id)
        if (filterContext.RouteData.Values.TryGetValue("partner_id", out var partnerIdObj))
        {
            if (int.TryParse(partnerIdObj?.ToString(), out var pId))
                return pId;
        }
        
        if (filterContext.RouteData.Values.TryGetValue("partnerId", out var partnerIdObj2))
        {
            if (int.TryParse(partnerIdObj2?.ToString(), out var pId))
                return pId;
        }

        // Try query string
        if (filterContext.HttpContext.Request.Query.TryGetValue("partnerId", out var partnerIdQuery))
        {
            if (int.TryParse(partnerIdQuery.FirstOrDefault(), out var pId))
                return pId;
        }

        // Try to get from contract_id or id in route (for contract operations)
        int? contractId = null;
        if (filterContext.RouteData.Values.TryGetValue("contract_id", out var contractIdObj))
        {
            if (int.TryParse(contractIdObj?.ToString(), out var cId))
                contractId = cId;
        }
        else if (filterContext.RouteData.Values.TryGetValue("id", out var idObj))
        {
            if (int.TryParse(idObj?.ToString(), out var cId))
                contractId = cId;
        }

        if (contractId.HasValue)
        {
            // Query database để lấy partnerId từ contractId
            using var scope = filterContext.HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CinemaDbCoreContext>();
            
            var partnerId = await dbContext.Contracts
                .Where(c => c.ContractId == contractId.Value)
                .Select(c => c.PartnerId)
                .FirstOrDefaultAsync();
            
            if (partnerId > 0)
                return partnerId;
        }

        // Try to get from request body (for POST/PUT requests)
        if (filterContext.HttpContext.Request.HasJsonContentType())
        {
            try
            {
                filterContext.HttpContext.Request.EnableBuffering();
                var bodyStream = filterContext.HttpContext.Request.Body;
                bodyStream.Position = 0;
                using var reader = new StreamReader(bodyStream, leaveOpen: true);
                var bodyText = await reader.ReadToEndAsync();
                bodyStream.Position = 0;

                // Simple JSON parsing to find partnerId
                if (!string.IsNullOrEmpty(bodyText))
                {
                    // Try to find "partnerId":123 or "partner_id":123
                    var partnerIdMatch = System.Text.RegularExpressions.Regex.Match(
                        bodyText, 
                        @"""partner[Ii]d""\s*:\s*(\d+)");
                    if (partnerIdMatch.Success && int.TryParse(partnerIdMatch.Groups[1].Value, out var pId))
                        return pId;
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return null;
    }
}








