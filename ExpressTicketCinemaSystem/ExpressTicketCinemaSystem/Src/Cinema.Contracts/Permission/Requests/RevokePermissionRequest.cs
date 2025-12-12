using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Requests;

/// <summary>
/// Request để Partner thu hồi quyền của Employee
/// </summary>
public class RevokePermissionRequest
{
    /// <summary>
    /// Danh sách ID của Cinema (null hoặc rỗng = thu hồi permission global)
    /// </summary>
    public List<int>? CinemaIds { get; set; }

    /// <summary>
    /// Danh sách Permission Codes cần thu hồi
    /// </summary>
    [Required(ErrorMessage = "Danh sách permissions là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 permission")]
    public List<string> PermissionCodes { get; set; } = new();
}

