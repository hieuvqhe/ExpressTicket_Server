using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirst("userId")?.Value
                        ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value
                           ?? Principal?.FindFirst("role")?.Value;

    public string? Username => Principal?.Identity?.Name
                               ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
}























