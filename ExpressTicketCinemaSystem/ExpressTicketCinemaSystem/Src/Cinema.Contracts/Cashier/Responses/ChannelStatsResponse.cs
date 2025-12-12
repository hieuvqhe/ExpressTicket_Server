using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

public class ChannelStatsResponse
{
    public int ShowtimeId { get; set; }
    public List<ChannelStat> Channels { get; set; } = new List<ChannelStat>();
}

public class ChannelStat
{
    public string ChannelName { get; set; } = null!; // App, Website, Partner, Counter
    public int TicketsSold { get; set; }
    public int TicketsCheckedIn { get; set; }
    public int NoShowCount { get; set; }
    public decimal CheckInRate { get; set; } // Percentage
    public decimal NoShowRate { get; set; } // Percentage
}























