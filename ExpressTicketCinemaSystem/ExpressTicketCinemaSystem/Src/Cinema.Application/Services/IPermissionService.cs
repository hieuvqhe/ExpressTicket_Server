using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

/// <summary>
/// Service quản lý Permission System
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Kiểm tra Employee có permission không
    /// </summary>
    Task<bool> HasPermissionAsync(int employeeId, int cinemaId, string permissionCode);

    /// <summary>
    /// Kiểm tra Employee có permission không (check multiple permissions - có 1 là đủ)
    /// </summary>
    Task<bool> HasAnyPermissionAsync(int employeeId, int cinemaId, params string[] permissionCodes);

    /// <summary>
    /// Kiểm tra Employee có tất cả permissions không
    /// </summary>
    Task<bool> HasAllPermissionsAsync(int employeeId, int cinemaId, params string[] permissionCodes);

    /// <summary>
    /// Cấp quyền cho Employee
    /// </summary>
    Task<PermissionActionResponse> GrantPermissionsAsync(int partnerId, int employeeId, GrantPermissionRequest request);

    /// <summary>
    /// Thu hồi quyền của Employee
    /// </summary>
    Task<PermissionActionResponse> RevokePermissionsAsync(int partnerId, int employeeId, RevokePermissionRequest request);

    /// <summary>
    /// Lấy danh sách permissions của Employee
    /// </summary>
    Task<EmployeePermissionsListResponse> GetEmployeePermissionsAsync(int employeeId, List<int>? cinemaIds = null);

    /// <summary>
    /// Lấy tất cả permissions có sẵn trong hệ thống
    /// </summary>
    Task<AvailablePermissionsResponse> GetAvailablePermissionsAsync();

    /// <summary>
    /// Lấy Employee ID từ User ID
    /// </summary>
    Task<int?> GetEmployeeIdByUserIdAsync(int userId);

    /// <summary>
    /// Kiểm tra Employee có quyền ở ít nhất 1 rạp được assign không (dùng cho GET ALL)
    /// </summary>
    Task<bool> HasAnyPermissionInAssignedCinemasAsync(int employeeId, params string[] permissionCodes);
}

