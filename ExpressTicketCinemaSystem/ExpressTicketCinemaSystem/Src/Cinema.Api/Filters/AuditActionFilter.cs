using System.IO;
using System.Text;
using System.Linq;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

public class AuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditActionFilter> _logger;
    private readonly string _action;
    private readonly string _tableName;
    private readonly string? _recordIdRouteKey;
    private readonly bool _includeRequestBody;

    public AuditActionFilter(
        IAuditLogService auditLogService,
        ILogger<AuditActionFilter> logger,
        string action,
        string tableName,
        string? recordIdRouteKey,
        bool includeRequestBody)
    {
        _auditLogService = auditLogService;
        _logger = logger;
        _action = action;
        _tableName = tableName;
        _recordIdRouteKey = string.IsNullOrWhiteSpace(recordIdRouteKey) ? null : recordIdRouteKey;
        _includeRequestBody = includeRequestBody;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? requestBody = null;
        if (_includeRequestBody)
        {
            requestBody = await ReadRequestBodyAsync(context.HttpContext.Request);
        }

        var executedContext = await next();

        if (executedContext.Exception != null)
        {
            return;
        }

        var statusCode = executedContext.HttpContext.Response?.StatusCode ?? StatusCodes.Status200OK;
        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return;
        }

        int? recordId = ExtractRecordId(context);

        var metadata = new
        {
            httpMethod = context.HttpContext.Request.Method,
            path = context.HttpContext.Request.Path.Value,
            query = context.HttpContext.Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
            routeValues = context.RouteData.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString()),
            requestBody,
            statusCode
        };

        try
        {
            await _auditLogService.LogEntityChangeAsync(
                action: _action,
                tableName: _tableName,
                recordId: recordId,
                beforeData: null,
                afterData: null,
                metadata: metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", _action);
        }
    }

    private int? ExtractRecordId(ActionExecutingContext context)
    {
        if (string.IsNullOrWhiteSpace(_recordIdRouteKey))
        {
            return null;
        }

        if (context.RouteData.Values.TryGetValue(_recordIdRouteKey!, out var value) &&
            int.TryParse(value?.ToString(), out var id))
        {
            return id;
        }

        return null;
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        if (request.ContentLength == null || request.ContentLength == 0)
        {
            return null;
        }

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }
}

