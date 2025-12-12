using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

/// <summary>
/// Attribute để yêu cầu permission cho ManagerStaff endpoint
/// Manager có toàn quyền và bỏ qua check này.
/// </summary>
public class RequireManagerStaffPermissionAttribute : TypeFilterAttribute
{
    public RequireManagerStaffPermissionAttribute(params string[] permissionCodes)
        : base(typeof(ManagerStaffPermissionAuthorizationFilter))
    {
        Arguments = new object[] { permissionCodes };
    }
}








