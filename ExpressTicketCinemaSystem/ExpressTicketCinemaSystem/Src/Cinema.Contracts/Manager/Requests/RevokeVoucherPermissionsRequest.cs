using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;

/// <summary>
/// Request để thu hồi nhiều Voucher permissions từ ManagerStaff cùng lúc
/// </summary>
public class RevokeVoucherPermissionsRequest
{
    /// <summary>
    /// Danh sách mã quyền Voucher cần thu hồi
    /// Ví dụ: ["VOUCHER_CREATE", "VOUCHER_READ", "VOUCHER_UPDATE", "VOUCHER_DELETE", "VOUCHER_SEND"]
    /// </summary>
    [Required(ErrorMessage = "Danh sách quyền không được để trống")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 quyền")]
    public List<string> PermissionCodes { get; set; } = new();
}







