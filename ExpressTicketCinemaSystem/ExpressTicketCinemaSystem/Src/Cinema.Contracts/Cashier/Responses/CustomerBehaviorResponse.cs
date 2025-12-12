using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

public class CustomerBehaviorResponse
{
    public int ShowtimeId { get; set; }
    public DateTime ShowtimeStart { get; set; }
    public BehaviorStats Stats { get; set; } = null!;
}

public class BehaviorStats
{
    public int TotalCheckIns { get; set; }
    public int EarlyArrivals { get; set; } // Đến trước 30 phút
    public int OnTimeArrivals { get; set; } // Đến trong 30 phút trước giờ chiếu
    public int LateArrivals { get; set; } // Đến sau khi phim đã chiếu
    public decimal EarlyArrivalRate { get; set; }
    public decimal OnTimeArrivalRate { get; set; }
    public decimal LateArrivalRate { get; set; }
    public List<CheckInTimeRange> TimeRanges { get; set; } = new List<CheckInTimeRange>();
}

public class CheckInTimeRange
{
    public string Range { get; set; } = null!; // ">30m before", "0-30m before", "after start"
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}























