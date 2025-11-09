using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses
{
    public class ShowtimeSeatMapResponse
    {
        public int ShowtimeId { get; set; }
        public MovieBrief Movie { get; set; } = new();
        public CinemaBrief Cinema { get; set; } = new();
        public ScreenBrief Screen { get; set; } = new();

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        public List<SeatTypeInfo> SeatTypes { get; set; } = new();
        public List<SeatCell> Seats { get; set; } = new();

        public DateTime ServerTime { get; set; }
    }

    public class MovieBrief { public int MovieId { get; set; } public string Title { get; set; } = ""; public string PosterUrl { get; set; } = ""; }
    public class CinemaBrief { public int CinemaId { get; set; } public string CinemaName { get; set; } = ""; public string City { get; set; } = ""; public string District { get; set; } = ""; }
    public class ScreenBrief { public int ScreenId { get; set; } public string ScreenName { get; set; } = ""; public string ScreenType { get; set; } = ""; public string SoundSystem { get; set; } = ""; }

    public class SeatTypeInfo
    {
        public int SeatTypeId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Surcharge { get; set; }
        public string Color { get; set; } = "#cccccc";
    }

    public class SeatCell
    {
        public int SeatId { get; set; }
        public string RowCode { get; set; } = "";
        public int SeatNumber { get; set; }
        public int SeatTypeId { get; set; }
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE|LOCKED|SOLD|BLOCKED
        public DateTime? LockedUntil { get; set; }
    }
}
