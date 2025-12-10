namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;

public class AdminAuditLogFilterRequest
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public int? UserId { get; set; }
    public string? Role { get; set; }
    public string? Action { get; set; }
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public string SortOrder { get; set; } = "desc";
}























