using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses
{
    public class MovieShowtimesOverviewResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public DateOnly Date { get; set; }

        public List<BrandItem> Brands { get; set; } = new();
        public PaginatedCinemas Cinemas { get; set; } = new();
    }

    public class BrandItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? LogoUrl { get; set; }
    }

    public class PaginatedCinemas
    {
        public List<MovieShowtimeCinemaGroup> Items { get; set; } = new();
        public PaginationMeta Pagination { get; set; } = new();
    }

    public class PaginationMeta
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class MovieShowtimeCinemaGroup
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string District { get; set; } = "";
        public string BrandCode { get; set; } = ""; // ví dụ từ Cinema.Code prefix
        public string? LogoUrl { get; set; }

        public List<MovieShowtimeScreenGroup> Screens { get; set; } = new();
    }

    public class MovieShowtimeScreenGroup
    {
        public int ScreenId { get; set; }
        public string ScreenName { get; set; } = "";
        public string ScreenType { get; set; } = "";
        public string SoundSystem { get; set; } = "";
        public int Capacity { get; set; }
        public List<MovieShowtimeItem> Showtimes { get; set; } = new();
    }

    public class MovieShowtimeItem
    {
        public int ShowtimeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string FormatType { get; set; } = "";
        public decimal BasePrice { get; set; }
        public int AvailableSeats { get; set; }
        public bool IsSoldOut { get; set; }
        public string Label { get; set; } = ""; // ex: "2D Phụ đề"
    }
}
