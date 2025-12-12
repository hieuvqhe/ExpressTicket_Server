using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

/// <summary>
/// Service quản lý Permission System cho ManagerStaff
/// </summary>
public interface IManagerStaffPermissionService
{
    /// <summary>
    /// Kiểm tra ManagerStaff có permission không
    /// </summary>
    Task<bool> HasPermissionAsync(int managerStaffId, int partnerId, string permissionCode);

    /// <summary>
    /// Kiểm tra ManagerStaff có permission không (check multiple permissions - có 1 là đủ)
    /// </summary>
    Task<bool> HasAnyPermissionAsync(int managerStaffId, int partnerId, params string[] permissionCodes);

    /// <summary>
    /// Kiểm tra ManagerStaff có tất cả permissions không
    /// </summary>
    Task<bool> HasAllPermissionsAsync(int managerStaffId, int partnerId, params string[] permissionCodes);

    /// <summary>
    /// Cấp quyền cho ManagerStaff
    /// </summary>
    Task<PermissionActionResponse> GrantPermissionsAsync(int managerId, int managerStaffId, GrantManagerStaffPermissionRequest request);

    /// <summary>
    /// Thu hồi quyền của ManagerStaff
    /// </summary>
    Task<PermissionActionResponse> RevokePermissionsAsync(int managerId, int managerStaffId, RevokeManagerStaffPermissionRequest request);

    /// <summary>
    /// Lấy danh sách permissions của ManagerStaff
    /// </summary>
    Task<ManagerStaffPermissionsListResponse> GetManagerStaffPermissionsAsync(int managerStaffId, List<int>? partnerIds = null);

    /// <summary>
    /// Lấy tất cả permissions có sẵn cho ManagerStaff
    /// </summary>
    Task<AvailablePermissionsResponse> GetAvailablePermissionsAsync();

    /// <summary>
    /// Lấy ManagerStaff ID từ User ID
    /// </summary>
    Task<int?> GetManagerStaffIdByUserIdAsync(int userId);

    /// <summary>
    /// Kiểm tra ManagerStaff có quyền ở ít nhất 1 partner được assign không
    /// </summary>
    Task<bool> HasAnyPermissionInAssignedPartnersAsync(int managerStaffId, params string[] permissionCodes);

    /// <summary>
    /// Kiểm tra ManagerStaff có Voucher permission không (GLOBAL - không cần partnerId)
    /// Chỉ 1 ManagerStaff có thể có Voucher permission tại 1 thời điểm
    /// </summary>
    Task<bool> HasVoucherPermissionAsync(int managerStaffId, string permissionCode);

    /// <summary>
    /// Cấp Voucher permission cho ManagerStaff (GLOBAL - không cần partnerId)
    /// Tự động revoke permission từ ManagerStaff khác nếu có
    /// </summary>
    Task<PermissionActionResponse> GrantVoucherPermissionAsync(int managerId, int managerStaffId, string permissionCode);

    /// <summary>
    /// Thu hồi Voucher permission từ ManagerStaff (GLOBAL - không cần partnerId)
    /// </summary>
    Task<PermissionActionResponse> RevokeVoucherPermissionAsync(int managerId, int managerStaffId, string permissionCode);

    /// <summary>
    /// Lấy ManagerStaff ID hiện có Voucher permission (nếu có)
    /// </summary>
    Task<int?> GetManagerStaffIdWithVoucherPermissionAsync();

    /// <summary>
    /// Cấp nhiều Voucher permissions cho ManagerStaff cùng lúc (GLOBAL - không cần partnerId)
    /// Tự động revoke permissions từ ManagerStaff khác nếu có
    /// Validate: Mỗi permission chỉ có thể được cấp cho 1 ManagerStaff duy nhất
    /// </summary>
    Task<PermissionActionResponse> GrantMultipleVoucherPermissionsAsync(int managerId, int managerStaffId, List<string> permissionCodes);

    /// <summary>
    /// Thu hồi nhiều Voucher permissions từ ManagerStaff cùng lúc (GLOBAL - không cần partnerId)
    /// </summary>
    Task<PermissionActionResponse> RevokeMultipleVoucherPermissionsAsync(int managerId, int managerStaffId, List<string> permissionCodes);
}

