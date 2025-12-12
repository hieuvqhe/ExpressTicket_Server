using System.Text.Json;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly CinemaDbCoreContext _context;
    private readonly ICurrentUserService _currentUserService;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditLogService(CinemaDbCoreContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task LogEntityChangeAsync(
        string action,
        string tableName,
        int? recordId,
        object? beforeData,
        object? afterData,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await WriteLogAsync(action, tableName, recordId, beforeData, afterData, metadata, cancellationToken);
    }

    public async Task LogCustomAsync(
        string action,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        object metaPayload = metadata == null
            ? new { message }
            : new { message, metadata };

        await WriteLogAsync(action, "System", null, null, null, metaPayload, cancellationToken);
    }

    public async Task<(List<AuditLog> Logs, int Total)> GetAuditLogsAsync(
        AdminAuditLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (filter.UserId.HasValue)
        {
            query = query.Where(l => l.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = filter.Role.Trim().ToLower();
            query = query.Where(l => l.Role != null && l.Role.ToLower() == role);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim().ToLower();
            query = query.Where(l => l.Action.ToLower().Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(filter.TableName))
        {
            var table = filter.TableName.Trim().ToLower();
            query = query.Where(l => l.TableName.ToLower().Contains(table));
        }

        if (filter.RecordId.HasValue)
        {
            query = query.Where(l => l.RecordId == filter.RecordId);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.Timestamp >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.Timestamp <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim().ToLower();
            query = query.Where(l =>
                (l.Metadata != null && l.Metadata.ToLower().Contains(keyword)) ||
                l.Action.ToLower().Contains(keyword) ||
                l.TableName.ToLower().Contains(keyword));
        }

        var total = await query.CountAsync(cancellationToken);

        var sortDescending = string.Equals(filter.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? false : true;
        query = sortDescending
            ? query.OrderByDescending(l => l.Timestamp)
            : query.OrderBy(l => l.Timestamp);

        var page = Math.Max(filter.Page, 1);
        var limit = Math.Clamp(filter.Limit, 1, 200);

        var logs = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (logs, total);
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(int logId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LogId == logId, cancellationToken);
    }

    private async Task WriteLogAsync(
        string action,
        string tableName,
        int? recordId,
        object? beforeData,
        object? afterData,
        object? metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name is required", nameof(tableName));
        }

        var log = new AuditLog
        {
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            UserId = _currentUserService.UserId,
            Role = _currentUserService.Role,
            IpAddress = _currentUserService.IpAddress,
            UserAgent = _currentUserService.UserAgent,
            BeforeData = Serialize(beforeData),
            AfterData = Serialize(afterData),
            Metadata = Serialize(metadata),
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? payload)
    {
        return payload == null ? null : JsonSerializer.Serialize(payload, SerializerOptions);
    }
}

