namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses
{
    public class MovieStatisticsResponse
    {
        public int TotalMovies { get; set; }
        public int NowShowingMovies { get; set; }
        public int ComingSoonMovies { get; set; }
        public int ActiveMovies { get; set; }
        public int InactiveMovies { get; set; }

        // ➕ thêm 2 thống kê từ bảng RatingFilm
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
    }
}
