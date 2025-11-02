namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class SeatLayoutActionResponse
    {
        public int ScreenId { get; set; }
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public int TotalSeats { get; set; }
        public int CreatedSeats { get; set; }
        public int UpdatedSeats { get; set; }
        public int BlockedSeats { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class SeatActionResponse
    {
        public int SeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Column { get; set; }

        public string? SeatName { get; set; }
        public int SeatTypeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class BulkSeatActionResponse
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<SeatActionResult> Results { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class SeatActionResult
    {
        public int SeatId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}