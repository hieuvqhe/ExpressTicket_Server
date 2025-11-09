namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests
{
    public class GetMovieShowtimesOverviewQuery
    {
        // BẮT BUỘC: yyyy-MM-dd
        public string Date { get; set; } = string.Empty;

        // FILTER tuỳ chọn
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Brand { get; set; }        // CGV|LOTTE|GALAXY|BHD|BETA...
        public int? CinemaId { get; set; }
        public string? ScreenType { get; set; }   // standard|imax|premium...
        public string? FormatType { get; set; }   // 2D|3D|4DX... (map từ showtime.format_type)
        public string? TimeFrom { get; set; }     // HH:mm (lọc theo giờ bắt đầu)
        public string? TimeTo { get; set; }       // HH:mm

        // PHÂN TRANG rạp
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;

        // SẮP XẾP rạp
        public string SortBy { get; set; } = "time";     // time|cinema|brand
        public string SortOrder { get; set; } = "asc";   // asc|desc
    }
}
