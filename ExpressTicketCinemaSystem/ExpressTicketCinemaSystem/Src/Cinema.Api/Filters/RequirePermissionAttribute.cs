using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

/// <summary>
/// Attribute để yêu cầu permission cho endpoint
/// Chỉ áp dụng cho Staff role. Partner có toàn quyền và bỏ qua check này.
/// </summary>
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(params string[] permissionCodes)
        : base(typeof(PermissionAuthorizationFilter))
    {
        Arguments = new object[] { permissionCodes };
    }
}





























