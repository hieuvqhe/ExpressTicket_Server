using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Requests;

/// <summary>
/// Request để Partner cấp quyền cho Employee
/// </summary>
public class GrantPermissionRequest
{
    /// <summary>
    /// Danh sách ID của Cinema (null hoặc rỗng = áp dụng cho tất cả cinemas của employee - global permission)
    /// </summary>
    public List<int>? CinemaIds { get; set; }

    /// <summary>
    /// Danh sách Permission Codes cần cấp
    /// </summary>
    [Required(ErrorMessage = "Danh sách permissions là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 permission")]
    public List<string> PermissionCodes { get; set; } = new();
}

