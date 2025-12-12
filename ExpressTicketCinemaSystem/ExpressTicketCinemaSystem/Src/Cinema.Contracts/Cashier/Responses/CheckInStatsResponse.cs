namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

public class CheckInStatsResponse
{
    public int ShowtimeId { get; set; }
    public string MovieName { get; set; } = null!;
    public DateTime ShowtimeStart { get; set; }
    public int TotalTicketsSold { get; set; }
    public int TotalTicketsCheckedIn { get; set; }
    public int NoShowCount { get; set; }
    public decimal CheckInRate { get; set; } // Percentage
    public decimal NoShowRate { get; set; } // Percentage
    public int OccupancyActual { get; set; } // Số ghế thực tế được sử dụng
    public int OccupancySold { get; set; } // Số ghế đã bán
}























