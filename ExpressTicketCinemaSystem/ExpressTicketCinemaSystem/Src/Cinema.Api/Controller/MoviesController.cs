using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controller
{
    [ApiController]
    [Route("cinema/movies")]
    [Produces("application/json")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly S3Service _s3Service;
        private readonly CinemaDbCoreContext _context;

        public MoviesController(IMovieService movieService, S3Service s3Service, CinemaDbCoreContext context)
        {
            _movieService = movieService;
            _s3Service = s3Service;
            _context = context;
        }

        /// <summary>
        /// Get all movies 
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="status">Filter by movie status</param>
        /// <param name="genre">Filter by movie genre (e.g., Action, Drama, Comedy)</param>
        /// <param name="language">Filter by movie language (e.g., English, Vietnamese, Japanese)</param>
        /// <param name="search">Search term for movie title, description, director, or cast</param>
        /// <param name="sortBy">Field to sort by</param>
        /// <param name="sortOrder">Sort order</param>
        /// <param name="premiereDateFrom">Filter by premiere date from (format: yyyy-MM-dd)</param>
        /// <param name="premiereDateTo">Filter by premiere date to (format: yyyy-MM-dd)</param>
        /// <param name="minRating">Minimum average rating filter (0.0 - 10.0)</param>
        /// <returns>Paginated list of movies</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedMoviesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] MovieEnums.MovieStatus? status = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? language = null,
            [FromQuery] string? search = null,
            [FromQuery(Name = "sort_by")] MovieEnums.SortBy sortBy = MovieEnums.SortBy.premiere_date,
            [FromQuery(Name = "sort_order")] MovieEnums.SortOrder sortOrder = MovieEnums.SortOrder.desc,
            [FromQuery(Name = "premiere_date_from")] DateOnly? premiereDateFrom = null,
            [FromQuery(Name = "premiere_date_to")] DateOnly? premiereDateTo = null,
            [FromQuery(Name = "min_rating")] double? minRating = null)
        {
            try
            {
                var result = await _movieService.GetAllMoviesAsync(page, limit,
                status?.ToString(),
                genre, language, search,
                sortBy.ToString(),
                sortOrder.ToString(),
                premiereDateFrom, premiereDateTo, minRating);

                // Dùng lớp Response đã định nghĩa
                var response = new PaginatedMoviesResponse
                {
                    Message = "Get movies success",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                return StatusCode(500, new ErrorResponse { Message = "An internal server error has occurred." });
            }
        }

        /// <summary>
        /// Search movies with advanced filtering
        /// </summary>
        /// <param name="search">Search query (title, description, director, actor)</param>
        /// <param name="genre">Filter by genre</param>
        /// <param name="year">Filter by release year</param>
        /// <param name="language">Filter by language</param>
        /// <param name="ratingMin">Minimum rating filter (0.0 - 10.0)</param>
        /// <param name="ratingMax">Maximum rating filter (0.0 - 10.0)</param>
        /// <param name="durationMin">Minimum duration in minutes</param>
        /// <param name="durationMax">Maximum duration in minutes</param>
        /// <param name="country">Filter by production country</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="sortBy">Field to sort by </param>
        /// <param name="sortOrder">Sort order </param>
        /// <returns>Paginated list of movies</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PaginatedMoviesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchMovies(
            [FromQuery] string? search = null,
            [FromQuery] string? genre = null,
            [FromQuery] int? year = null,
            [FromQuery] string? language = null,
            [FromQuery(Name = "rating_min")] double? ratingMin = null,
            [FromQuery(Name = "rating_max")] double? ratingMax = null,
            [FromQuery(Name = "duration_min")] int? durationMin = null,
            [FromQuery(Name = "duration_max")] int? durationMax = null,
            [FromQuery] string? country = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery(Name = "sort_by")] MovieEnums.SortBy sortBy = MovieEnums.SortBy.title,
            [FromQuery(Name = "sort_order")] MovieEnums.SortOrder sortOrder = MovieEnums.SortOrder.asc)
        {
            try
            {
                var result = await _movieService.SearchMoviesAsync(search, genre, year, language, ratingMin, ratingMax,
                durationMin, durationMax, country, page, limit, sortBy.ToString(), sortOrder.ToString());

                // Dùng lớp Response đã định nghĩa
                var response = new PaginatedMoviesResponse
                {
                    Message = "Get movies success",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                return StatusCode(500, new ErrorResponse { Message = "An internal server error has occurred." });
            }
        }
        /// <summary>
        /// Get now showing movies with advanced filtering and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="genre">Filter by genre</param>
        /// <param name="language">Filter by language</param>
        /// <param name="ratingMin">Minimum rating filter (0.0 - 10.0)</param>
        /// <param name="durationMin">Minimum duration in minutes</param>
        /// <param name="durationMax">Maximum duration in minutes</param>
        /// <param name="sortBy">Field to sort by</param>
        /// <param name="sortOrder">Sort order</param>
        /// <returns>Paginated list of now showing movies</returns>
        [HttpGet("categories/now-showing")]
        [ProducesResponseType(typeof(PaginatedMoviesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNowShowing(
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10,
    [FromQuery] string? genre = null,
    [FromQuery] string? language = null,
    [FromQuery(Name = "rating_min")] double? ratingMin = null,
    [FromQuery(Name = "duration_min")] int? durationMin = null,
    [FromQuery(Name = "duration_max")] int? durationMax = null,
    [FromQuery(Name = "sort_by")] MovieEnums.SortBy sortBy = MovieEnums.SortBy.premiere_date,
    [FromQuery(Name = "sort_order")] MovieEnums.SortOrder sortOrder = MovieEnums.SortOrder.desc)
        {
            try
            {
                var result = await _movieService.GetNowShowingMoviesAsync(
                    page, limit, genre, language, ratingMin, durationMin, durationMax,
                    sortBy.ToString(), sortOrder.ToString());

                var response = new PaginatedMoviesResponse
                {
                    Message = "Get now showing movies success",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "An internal server error occurred." });
            }
        }
        /// <summary>
        /// Get coming soon movies with advanced filtering and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="genre">Filter by genre</param>
        /// <param name="language">Filter by language</param>
        /// <param name="ratingMin">Minimum rating filter (0.0 - 10.0)</param>
        /// <param name="durationMin">Minimum duration in minutes</param>
        /// <param name="durationMax">Maximum duration in minutes</param>
        /// <param name="premiereDateFrom">Filter by premiere date from (format: yyyy-MM-dd)</param>
        /// <param name="premiereDateTo">Filter by premiere date to (format: yyyy-MM-dd)</param>
        /// <param name="sortBy">Field to sort by</param>
        /// <param name="sortOrder">Sort order</param>
        /// <returns>Paginated list of coming soon movies</returns>
        [HttpGet("categories/coming-soon")]
        [ProducesResponseType(typeof(PaginatedMoviesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetComingSoon(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? genre = null,
            [FromQuery] string? language = null,
            [FromQuery(Name = "rating_min")] double? ratingMin = null,
            [FromQuery(Name = "duration_min")] int? durationMin = null,
            [FromQuery(Name = "duration_max")] int? durationMax = null,
            [FromQuery(Name = "premiere_date_from")] DateOnly? premiereDateFrom = null,
            [FromQuery(Name = "premiere_date_to")] DateOnly? premiereDateTo = null,
            [FromQuery(Name = "sort_by")] MovieEnums.SortBy sortBy = MovieEnums.SortBy.premiere_date,
            [FromQuery(Name = "sort_order")] MovieEnums.SortOrder sortOrder = MovieEnums.SortOrder.asc)
        {
            try
            {
                var result = await _movieService.GetComingSoonMoviesAsync(
                    page, limit, genre, language, ratingMin, durationMin, durationMax,
                    premiereDateFrom, premiereDateTo, sortBy.ToString(), sortOrder.ToString());

                var response = new PaginatedMoviesResponse
                {
                    Message = "Get coming soon movies success",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Get all available movie genres with count
        /// </summary>
        /// <returns>List of genres with movie count</returns>
        [HttpGet("meta/genres")]
        public async Task<ActionResult> GetAvailableGenres()
        {
            var result = await _movieService.GetAvailableGenresAsync();

            return Ok(new
            {
                message = "Get available genres success",
                result = result
            });
        }

        /// <summary>
        /// Get all available movie languages with count
        /// </summary>
        /// <returns>List of languages with movie count</returns>
        [HttpGet("meta/languages")]
        public async Task<ActionResult> GetAvailableLanguages()
        {
            var result = await _movieService.GetAvailableLanguagesAsync();

            return Ok(new
            {
                message = "Get available language success",
                result = result
            });
        }


        /// <summary>
        /// Get movies by specific genre with pagination
        /// </summary>
        /// <param name="genre">Genre name to filter by (e.g., Action, Drama, Comedy)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="sortBy">Field to sort by (default: title)</param>
        /// <param name="sortOrder">Sort order (default: asc)</param>
        /// <returns>Paginated list of movies by genre</returns>
        [HttpGet("genre/{genre}")]
        public async Task<IActionResult> GetMoviesByGenre(
            [FromRoute] string genre,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery(Name = "sort_by")] MovieEnums.SortBy sortBy = MovieEnums.SortBy.title,
            [FromQuery(Name = "sort_order")] MovieEnums.SortOrder sortOrder = MovieEnums.SortOrder.asc)
        {
            try
            {
                var resultByGenre = await _movieService.GetMoviesByGenreAsync(genre, page, limit, sortBy.ToString(), sortOrder.ToString());

                var response = new PaginatedMoviesResponse
                {
                    Message = $"Get {genre} movies success",
                    Result = new MoviePaginatedResponse
                    {
                        Movies = resultByGenre.Movies,
                        Page = resultByGenre.Page,
                        Limit = resultByGenre.Limit,
                        Total = resultByGenre.Total,
                        TotalPages = resultByGenre.TotalPages
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "An internal server error occurred." });
            }
        }

        /// <summary>
        /// Get movie statistics and analytics
        /// </summary>
        /// <returns>Movie statistics including counts, ratings, and status breakdown</returns>
        [HttpGet("meta/stats")]
        public async Task<ActionResult<MovieStatisticsResponse>> GetMovieStats()
        {
            var stats = await _movieService.GetMovieStatisticsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Get top rated movies with filtering options
        /// </summary>
        /// <param name="limit">Number of movies to return (default: 10)</param>
        /// <param name="minRatingsCount">Minimum number of ratings required (default: 1)</param>
        /// <param name="timePeriod">Time period for ratings (all, week, month, year) (default: all)</param>
        /// <returns>List of top rated movies</returns>
        [HttpGet("categories/top-rated")]
        public async Task<IActionResult> GetTopRatedMovies(
            [FromQuery] int limit = 10,
            [FromQuery(Name = "min_ratings_count")] int minRatingsCount = 1,
            [FromQuery(Name = "time_period")] string timePeriod = "all")
        {
            var result = await _movieService.GetTopRatedMoviesAsync(limit, minRatingsCount, timePeriod);
            return Ok(new
            {
                message = "Get top rated movies success",
                result
            });
        }
        /// <summary>
        /// Get movie details by ID
        /// </summary>
        /// <param name="id">Movie ID</param>
        /// <returns>Movie details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PaginatedMoviesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMovieById([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse { Message = "Invalid movie ID" });
                }

                var movie = await _movieService.GetMovieByIdAsync(id);

                return Ok(new
                {
                    message = "Get movie details success",
                    result = movie
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "An internal server error occurred." });
            }
        }


        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var url = await _s3Service.UploadFileAsync(file);
            return Ok(new { ImageUrl = url });
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var url = await _s3Service.UploadVideoAsync(file);
            return Ok(new { url });
        }

    }

}
