using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class PointHistory
{
    public long PointHistoryId { get; set; }

    public int CustomerId { get; set; }

    public string? OrderId { get; set; } // OrderId nếu tích điểm từ đơn hàng

    public string TransactionType { get; set; } = null!; // EARNED, USED, EXPIRED, BONUS

    public int Points { get; set; } // Số điểm (dương nếu EARNED/BONUS, âm nếu USED/EXPIRED)

    public string? Description { get; set; }

    public int? VipLevelId { get; set; } // VIP level tại thời điểm giao dịch

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual VIPLevel? VIPLevel { get; set; }
}


















