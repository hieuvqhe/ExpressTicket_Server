using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

/// <summary>
/// Attribute để yêu cầu Voucher permission cho ManagerStaff endpoint
/// Manager có toàn quyền và bỏ qua check này.
/// Voucher permissions are GLOBAL (không cần partnerId) - chỉ 1 ManagerStaff có thể có quyền tại 1 thời điểm
/// </summary>
public class RequireVoucherPermissionAttribute : TypeFilterAttribute
{
    public RequireVoucherPermissionAttribute(params string[] permissionCodes)
        : base(typeof(VoucherPermissionAuthorizationFilter))
    {
        Arguments = new object[] { permissionCodes };
    }
}
