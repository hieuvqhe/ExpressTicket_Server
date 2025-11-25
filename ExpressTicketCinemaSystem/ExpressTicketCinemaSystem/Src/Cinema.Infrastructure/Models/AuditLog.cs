using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string? Role { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public int? RecordId { get; set; }

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }
}
