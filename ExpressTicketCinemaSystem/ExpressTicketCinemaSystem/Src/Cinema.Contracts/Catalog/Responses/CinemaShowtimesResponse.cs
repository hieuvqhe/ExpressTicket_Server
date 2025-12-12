using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses
{
    public class CinemaShowtimesResponse
    {
        public DateOnly Date { get; set; }
        public List<BrandItem> Brands { get; set; } = new();
        public PaginatedCinemaShowtimes Cinemas { get; set; } = new();
    }

    public class PaginatedCinemaShowtimes
    {
        public List<CinemaShowtimeGroup> Items { get; set; } = new();
        public PaginationMeta Pagination { get; set; } = new();
    }

    public class CinemaShowtimeGroup
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string District { get; set; } = "";
        public string BrandCode { get; set; } = "";
        public string? LogoUrl { get; set; }

        // Thông tin các phim đang chiếu tại rạp này
        public List<MovieShowtimeGroup> Movies { get; set; } = new();
    }

    public class MovieShowtimeGroup
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public string Duration { get; set; } = "";
        public string AgeRating { get; set; } = "";

        // Các suất chiếu theo phòng
        public List<CinemaScreenShowtimeGroup> Screens { get; set; } = new();
    }

    public class CinemaScreenShowtimeGroup
    {
        public int ScreenId { get; set; }
        public string ScreenName { get; set; } = "";
        public string ScreenType { get; set; } = "";
        public string SoundSystem { get; set; } = "";

        public List<ShowtimeBriefItem> Showtimes { get; set; } = new();
    }

    public class ShowtimeBriefItem
    {
        public int ShowtimeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string FormatType { get; set; } = "";
        public decimal BasePrice { get; set; }
        public int AvailableSeats { get; set; }
        public bool IsSoldOut { get; set; }
        public string Label { get; set; } = "";
    }
}