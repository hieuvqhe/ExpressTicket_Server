using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests
{
    public class GetCinemaShowtimesQuery
    {
        public string Date { get; set; } = ""; // yyyy-MM-dd
        public int? MovieId { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Brand { get; set; }
        public string? ScreenType { get; set; }
        public string? FormatType { get; set; }
        public string? TimeFrom { get; set; } // HH:mm
        public string? TimeTo { get; set; } // HH:mm

        // Pagination
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;

        // Sort: time, cinema, movie
        public string SortBy { get; set; } = "time";
        public string SortOrder { get; set; } = "asc";
    }
}