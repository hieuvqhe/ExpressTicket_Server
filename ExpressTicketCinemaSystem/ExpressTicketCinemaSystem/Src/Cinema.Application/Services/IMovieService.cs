using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IMovieService
    {
        Task<IEnumerable<MovieResponse>> GetAllMoviesAsync();
        Task<IEnumerable<MovieResponse>> SearchMoviesAsync(string? title, string? genre, int? year, string? actorName);
        Task<IEnumerable<MovieResponse>> GetNowShowingMoviesAsync();
        Task<IEnumerable<MovieResponse>> GetComingSoonMoviesAsync();
        Task<IEnumerable<string>> GetAvailableGenresAsync();
        Task<IEnumerable<string>> GetAvailableLanguagesAsync();
        Task<IEnumerable<MovieResponse>> GetMoviesByGenreAsync(string genre);
        Task<MovieStatisticsResponse> GetMovieStatisticsAsync();
        Task<IEnumerable<TopRatedMovieResponse>> GetTopRatedMoviesAsync(int top = 10);


    }

}
