using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

public class AuditActionAttribute : TypeFilterAttribute
{
    public AuditActionAttribute(
        string action,
        string tableName,
        string? recordIdRouteKey = null,
        bool includeRequestBody = true)
        : base(typeof(AuditActionFilter))
    {
        var routeKey = string.IsNullOrWhiteSpace(recordIdRouteKey) ? string.Empty : recordIdRouteKey;
        Arguments = new object[] { action, tableName, routeKey, includeRequestBody };
    }
}

