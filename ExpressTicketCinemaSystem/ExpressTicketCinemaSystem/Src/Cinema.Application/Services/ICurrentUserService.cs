using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Role { get; }
    string? Username { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    ClaimsPrincipal? Principal { get; }
}























