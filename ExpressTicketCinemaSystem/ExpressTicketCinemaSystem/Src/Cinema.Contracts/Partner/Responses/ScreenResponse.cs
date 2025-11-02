using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class ScreenResponse
    {
        public int ScreenId { get; set; }
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ScreenType { get; set; } = string.Empty;
        public string? SoundSystem { get; set; }
        public int Capacity { get; set; }
        public int SeatRows { get; set; }
        public int SeatColumns { get; set; }
        public bool IsActive { get; set; }
        public bool HasSeatLayout { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

public class ScreenActionResponse
{
    public int ScreenId { get; set; }
    public string ScreenName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class PaginatedScreensResponse
{
    public List<ScreenResponse> Screens { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}