using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;

/// <summary>
/// Request để Manager cấp quyền cho ManagerStaff
/// </summary>
public class GrantManagerStaffPermissionRequest
{
    /// <summary>
    /// Danh sách ID của Partner (null hoặc rỗng = áp dụng cho tất cả partners được assign cho staff - global permission)
    /// </summary>
    public List<int>? PartnerIds { get; set; }

    /// <summary>
    /// Danh sách Permission Codes cần cấp
    /// </summary>
    [Required(ErrorMessage = "Danh sách permissions là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 permission")]
    public List<string> PermissionCodes { get; set; } = new();
}








