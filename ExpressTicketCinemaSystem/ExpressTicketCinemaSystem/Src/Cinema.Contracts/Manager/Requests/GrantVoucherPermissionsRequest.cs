using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;

/// <summary>
/// Request để cấp nhiều Voucher permissions cho ManagerStaff cùng lúc
/// </summary>
public class GrantVoucherPermissionsRequest
{
    /// <summary>
    /// Danh sách mã quyền Voucher cần cấp
    /// Ví dụ: ["VOUCHER_CREATE", "VOUCHER_READ", "VOUCHER_UPDATE", "VOUCHER_DELETE", "VOUCHER_SEND"]
    /// </summary>
    [Required(ErrorMessage = "Danh sách quyền không được để trống")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 quyền")]
    public List<string> PermissionCodes { get; set; } = new();
}








