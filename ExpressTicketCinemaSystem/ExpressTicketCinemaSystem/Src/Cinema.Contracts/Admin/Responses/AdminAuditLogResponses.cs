namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses;

public class AdminAuditLogListResponse
{
    public string Message { get; set; } = "Lấy audit log thành công";
    public AdminAuditLogListResult Result { get; set; } = new();
}

public class AdminAuditLogListResult
{
    public List<AdminAuditLogItemResponse> Logs { get; set; } = new();
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class AdminAuditLogItemResponse
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string? Role { get; set; }
    public string Action { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public int? RecordId { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Before { get; set; }
    public object? After { get; set; }
    public object? Metadata { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AdminAuditLogDetailResponse
{
    public string Message { get; set; } = "Lấy chi tiết audit log thành công";
    public AdminAuditLogItemResponse Result { get; set; } = new();
}









