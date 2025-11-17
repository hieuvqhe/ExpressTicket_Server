using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IMovieService
    {
        Task<MoviePaginatedResponse> GetAllMoviesAsync(
            int page = 1,
            int limit = 10,
            string? status = null,
            string? genre = null,
            string? language = null,
            string? search = null,
            string sortBy = "premiere_date",
            string sortOrder = "desc",
            DateOnly? premiereDateFrom = null,
            DateOnly? premiereDateTo = null,
            double? minRating = null);
        Task<MoviePaginatedResponse> SearchMoviesAsync(
            string? search = null,
            string? genre = null,
            int? year = null,
            string? language = null,
            double? ratingMin = null,
            double? ratingMax = null,
            int? durationMin = null,
            int? durationMax = null,
            string? country = null,
            int page = 1,
            int limit = 10,
            string sortBy = "title",
            string sortOrder = "asc");
        Task<MoviePaginatedResponse> GetNowShowingMoviesAsync(
            int page = 1,
            int limit = 10,
            string? genre = null,
            string? language = null,
            double? ratingMin = null,
            int? durationMin = null,
            int? durationMax = null,
            string sortBy = "premiere_date",
            string sortOrder = "desc");
        Task<MoviePaginatedResponse> GetComingSoonMoviesAsync(
            int page = 1,
            int limit = 10,
            string? genre = null,
            string? language = null,
            double? ratingMin = null,
            int? durationMin = null,
            int? durationMax = null,
            DateOnly? premiereDateFrom = null,
            DateOnly? premiereDateTo = null,
            string sortBy = "premiere_date",
            string sortOrder = "asc");
        Task<IEnumerable<GenreResponse>> GetAvailableGenresAsync();
        Task<IEnumerable<LanguageCountResponse>> GetAvailableLanguagesAsync();
        Task<MovieResponse> GetMovieByIdAsync(int movieId);
        Task<MoviePaginatedByGenreResponse> GetMoviesByGenreAsync(string genre, int page, int limit, string sortBy, string sortOrder);
        Task<MovieStatisticsResponse> GetMovieStatisticsAsync();
        Task<IEnumerable<TopRatedMovieResponse>> GetTopRatedMoviesAsync(int limit = 10, int minRatingsCount = 1, string timePeriod = "all");
        Task<(bool success, string message, RatingFilm? rating)> CreateReviewAsync(int movieId, int userId, int ratingStar, string comment);
        Task<(bool success, string message, RatingFilm? rating)> UpdateReviewAsync(int movieId, int userId, int ratingStar, string comment);
        Task<(bool success, string message)> DeleteReviewAsync(int movieId, int userId);
        Task<(bool success, string message, GetMovieReviewsResponse? data)> GetMovieReviewsAsync(
            int movieId,
            int page = 1,
            int limit = 10,
            string sort = "newest");
        Task<(bool success, string message, GetMovieRatingSummaryResponse? data)> GetMovieRatingSummaryAsync(int movieId);
        Task<(bool success, string message, GetMyReviewResponse? data)> GetMyReviewAsync(int movieId, int userId);
    }
}