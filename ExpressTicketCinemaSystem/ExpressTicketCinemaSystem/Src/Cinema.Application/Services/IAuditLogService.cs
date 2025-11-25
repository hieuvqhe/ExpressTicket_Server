using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public interface IAuditLogService
{
    Task LogEntityChangeAsync(
        string action,
        string tableName,
        int? recordId,
        object? beforeData,
        object? afterData,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task LogCustomAsync(
        string action,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<(List<AuditLog> Logs, int Total)> GetAuditLogsAsync(
        AdminAuditLogFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetAuditLogByIdAsync(int logId, CancellationToken cancellationToken = default);
}

