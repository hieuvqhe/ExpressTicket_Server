using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;

/// <summary>
/// Request để Manager thu hồi quyền của ManagerStaff
/// </summary>
public class RevokeManagerStaffPermissionRequest
{
    /// <summary>
    /// Danh sách ID của Partner (null hoặc rỗng = thu hồi permission global)
    /// </summary>
    public List<int>? PartnerIds { get; set; }

    /// <summary>
    /// Danh sách Permission Codes cần thu hồi
    /// </summary>
    [Required(ErrorMessage = "Danh sách permissions là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 permission")]
    public List<string> PermissionCodes { get; set; } = new();
}








