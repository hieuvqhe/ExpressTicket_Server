namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;

public class PointHistoryResponse
{
    public List<PointHistoryItem> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}

public class PointHistoryItem
{
    public long PointHistoryId { get; set; }
    public string? OrderId { get; set; }
    public string TransactionType { get; set; } = null!;
    public int Points { get; set; }
    public string? Description { get; set; }
    public string? VipLevelName { get; set; }
    public DateTime CreatedAt { get; set; }
}










