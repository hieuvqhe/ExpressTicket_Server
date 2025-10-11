using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IMovieService
    {
        Task<IEnumerable<MovieResponse>> GetAllMoviesAsync();
        Task<IEnumerable<MovieResponse>> SearchMoviesAsync(string? title, string? genre, int? year, string? actorName);
        Task<MoviePaginatedResponse> GetNowShowingMoviesAsync(int page, int limit, string sortBy, string sortOrder);
        Task<MoviePaginatedResponse> GetComingSoonMoviesAsync(int page, int limit, string sortBy, string sortOrder);
        Task<IEnumerable<GenreCountDto>> GetAvailableGenresAsync();
        Task<IEnumerable<LanguageCountDto>> GetAvailableLanguagesAsync();
        Task<MoviePaginatedByGenreResponse> GetMoviesByGenreAsync(string genre, int page, int limit, string sortBy, string sortOrder);

        Task<MovieStatisticsResponse> GetMovieStatisticsAsync();
        Task<IEnumerable<TopRatedMovieResponse>> GetTopRatedMoviesAsync(int limit = 10, int minRatingsCount = 1, string timePeriod = "all");



    }

}
