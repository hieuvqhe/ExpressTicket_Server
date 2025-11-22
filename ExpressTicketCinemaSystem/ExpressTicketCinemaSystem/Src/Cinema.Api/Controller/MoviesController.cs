using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controller
{
    [ApiController]
    [Route("cinema/movies")]
    [Produces("application/json")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
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

        /// <summary>
        /// Create a review for a movie
        /// </summary>
        /// <remarks>
        /// User must be authenticated, have confirmed email, active account, and have purchased and paid for a ticket to this movie
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        /// <param name="request">Review details (rating_star: 1-5, comment: 1-1000 characters)</param>
        [HttpPost("{movieId}/reviews")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<CreateReviewResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateReview(
            int movieId,
            [FromBody] CreateReviewRequest request)
        {
            try
            {
                // Validate rating_star phải trong khoảng 1-5
                if (request.RatingStar < 1 || request.RatingStar > 5)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["rating_star"] = new ValidationError
                            {
                                Msg = "Số sao đánh giá phải từ 1 đến 5",
                                Path = "rating_star",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate comment không rỗng
                if (string.IsNullOrWhiteSpace(request.Comment))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["comment"] = new ValidationError
                            {
                                Msg = "Bình luận không được để trống",
                                Path = "comment",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate độ dài comment (1-1000 ký tự)
                if (request.Comment.Length > 1000)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["comment"] = new ValidationError
                            {
                                Msg = "Bình luận không được vượt quá 1000 ký tự",
                                Path = "comment",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate image URLs (tối đa 3 ảnh)
                if (request.ImageUrls != null && request.ImageUrls.Count > 3)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["image_urls"] = new ValidationError
                            {
                                Msg = "Tối đa 3 ảnh được phép",
                                Path = "image_urls",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate image URL format (nếu có)
                if (request.ImageUrls != null && request.ImageUrls.Any())
                {
                    var urlPattern = @"^https?://.+\.(jpg|jpeg|png)$";
                    var invalidUrls = request.ImageUrls
                        .Where(url => string.IsNullOrWhiteSpace(url) || !Regex.IsMatch(url, urlPattern, RegexOptions.IgnoreCase))
                        .ToList();

                    if (invalidUrls.Any())
                    {
                        return BadRequest(new ValidationErrorResponse
                        {
                            Message = "Lỗi xác thực dữ liệu",
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["image_urls"] = new ValidationError
                                {
                                    Msg = "URL ảnh không hợp lệ. Phải là URL ảnh (jpg, jpeg, png)",
                                    Path = "image_urls",
                                    Location = "body"
                                }
                            }
                        });
                    }

                    // Kiểm tra trùng lặp
                    if (request.ImageUrls.Distinct().Count() != request.ImageUrls.Count)
                    {
                        return BadRequest(new ValidationErrorResponse
                        {
                            Message = "Lỗi xác thực dữ liệu",
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["image_urls"] = new ValidationError
                                {
                                    Msg = "Không được phép có URL ảnh trùng lặp",
                                    Path = "image_urls",
                                    Location = "body"
                                }
                            }
                        });
                    }
                }

                // Lấy userId từ token
                var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ValidationErrorResponse
                    {
                        Message = "Xác thực thất bại",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["auth"] = new ValidationError
                            {
                                Msg = "Không thể xác định người dùng từ token",
                                Path = "token",
                                Location = "header"
                            }
                        }
                    });
                }

                // Gọi service để tạo review
                var (success, message, rating) = await _movieService.CreateReviewAsync(
                    movieId, 
                    userId, 
                    request.RatingStar, 
                    request.Comment,
                    request.ImageUrls);

                if (!success)
                {
                    // Xác định loại lỗi dựa trên message
                    if (message.Contains("Không tìm thấy phim"))
                    {
                        return NotFound(new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["movieId"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }
                    else if (message.Contains("đã đánh giá"))
                    {
                        return Conflict(new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["review"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }
                    else if (message.Contains("mua vé"))
                    {
                        return StatusCode(403, new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["permission"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }

                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["error"] = new ValidationError
                            {
                                Msg = message,
                                Path = "request",
                                Location = "body"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<CreateReviewResponse>
                {
                    Message = message,
                    Result = new CreateReviewResponse
                    {
                        RatingId = rating!.RatingId,
                        MovieId = rating.MovieId,
                        UserId = rating.UserId,
                        UserName = rating.User?.Fullname ?? "Ẩn danh",
                        UserAvatar = rating.User?.AvatarUrl,
                        RatingStar = rating.RatingStar,
                        Comment = rating.Comment,
                        RatingAt = rating.RatingAt,
                        ImageUrls = !string.IsNullOrEmpty(rating.ImageUrls) 
                            ? rating.ImageUrls.Split(';').ToList() 
                            : new List<string>()
                    }
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình tạo review."
                });
            }
        }

        /// <summary>
        /// Update an existing review for a movie
        /// </summary>
        /// <remarks>
        /// User must be authenticated and must have previously reviewed this movie
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        /// <param name="request">Updated review details (rating_star: 1-5, comment: 1-1000 characters)</param>
        [HttpPut("{movieId}/reviews")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<CreateReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateReview(
            int movieId,
            [FromBody] CreateReviewRequest request)
        {
            try
            {
                // Validate rating_star phải trong khoảng 1-5
                if (request.RatingStar < 1 || request.RatingStar > 5)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["rating_star"] = new ValidationError
                            {
                                Msg = "Số sao đánh giá phải từ 1 đến 5",
                                Path = "rating_star",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate comment không rỗng
                if (string.IsNullOrWhiteSpace(request.Comment))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["comment"] = new ValidationError
                            {
                                Msg = "Bình luận không được để trống",
                                Path = "comment",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate độ dài comment (1-1000 ký tự)
                if (request.Comment.Length > 1000)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["comment"] = new ValidationError
                            {
                                Msg = "Bình luận không được vượt quá 1000 ký tự",
                                Path = "comment",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate image URLs (tối đa 3 ảnh)
                if (request.ImageUrls != null && request.ImageUrls.Count > 3)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["image_urls"] = new ValidationError
                            {
                                Msg = "Tối đa 3 ảnh được phép",
                                Path = "image_urls",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate image URL format (nếu có)
                if (request.ImageUrls != null && request.ImageUrls.Any())
                {
                    var urlPattern = @"^https?://.+\.(jpg|jpeg|png)$";
                    var invalidUrls = request.ImageUrls
                        .Where(url => string.IsNullOrWhiteSpace(url) || !Regex.IsMatch(url, urlPattern, RegexOptions.IgnoreCase))
                        .ToList();

                    if (invalidUrls.Any())
                    {
                        return BadRequest(new ValidationErrorResponse
                        {
                            Message = "Lỗi xác thực dữ liệu",
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["image_urls"] = new ValidationError
                                {
                                    Msg = "URL ảnh không hợp lệ. Phải là URL ảnh (jpg, jpeg, png)",
                                    Path = "image_urls",
                                    Location = "body"
                                }
                            }
                        });
                    }

                    // Kiểm tra trùng lặp
                    if (request.ImageUrls.Distinct().Count() != request.ImageUrls.Count)
                    {
                        return BadRequest(new ValidationErrorResponse
                        {
                            Message = "Lỗi xác thực dữ liệu",
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["image_urls"] = new ValidationError
                                {
                                    Msg = "Không được phép có URL ảnh trùng lặp",
                                    Path = "image_urls",
                                    Location = "body"
                                }
                            }
                        });
                    }
                }

                // Lấy userId từ token
                var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ValidationErrorResponse
                    {
                        Message = "Xác thực thất bại",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["auth"] = new ValidationError
                            {
                                Msg = "Không thể xác định người dùng từ token",
                                Path = "token",
                                Location = "header"
                            }
                        }
                    });
                }

                // Gọi service để update review
                var (success, message, rating) = await _movieService.UpdateReviewAsync(
                    movieId,
                    userId,
                    request.RatingStar,
                    request.Comment,
                    request.ImageUrls);

                if (!success)
                {
                    // Xác định loại lỗi dựa trên message
                    if (message.Contains("Không tìm thấy phim"))
                    {
                        return NotFound(new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["movieId"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }
                    else if (message.Contains("chưa review"))
                    {
                        return NotFound(new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["review"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }

                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["error"] = new ValidationError
                            {
                                Msg = message,
                                Path = "request",
                                Location = "body"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<CreateReviewResponse>
                {
                    Message = message,
                    Result = new CreateReviewResponse
                    {
                        RatingId = rating!.RatingId,
                        MovieId = rating.MovieId,
                        UserId = rating.UserId,
                        UserName = rating.User?.Fullname ?? "Ẩn danh",
                        UserAvatar = rating.User?.AvatarUrl,
                        RatingStar = rating.RatingStar,
                        Comment = rating.Comment,
                        RatingAt = rating.RatingAt,
                        ImageUrls = !string.IsNullOrEmpty(rating.ImageUrls) 
                            ? rating.ImageUrls.Split(';').ToList() 
                            : new List<string>()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình cập nhật review."
                });
            }
        }

        /// <summary>
        /// Get reviews for a movie (Public - no authentication required)
        /// </summary>
        /// <remarks>
        /// Retrieve paginated list of reviews for a specific movie. Supports sorting by newest, oldest, highest rating, or lowest rating. Only shows active (non-deleted) reviews.
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of reviews per page (default: 10)</param>
        /// <param name="sort">Sort type: newest (default), oldest, highest, lowest</param>
        [HttpGet("{movieId}/reviews")]
        [ProducesResponseType(typeof(SuccessResponse<GetMovieReviewsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieReviews(
            int movieId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string sort = "newest")
        {
            try
            {
                // Validate movieId
                if (movieId <= 0)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movieId"] = new ValidationError
                            {
                                Msg = "Movie ID phải là số nguyên dương",
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Validate page và limit
                if (page < 1)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["page"] = new ValidationError
                            {
                                Msg = "Page phải lớn hơn hoặc bằng 1",
                                Path = "page",
                                Location = "query"
                            }
                        }
                    });
                }

                if (limit < 1 || limit > 100)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["limit"] = new ValidationError
                            {
                                Msg = "Limit phải trong khoảng 1-100",
                                Path = "limit",
                                Location = "query"
                            }
                        }
                    });
                }

                // Validate sort
                var validSorts = new[] { "newest", "oldest", "highest", "lowest" };
                if (!validSorts.Contains(sort.ToLower()))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["sort"] = new ValidationError
                            {
                                Msg = "Sort phải là: newest, oldest, highest, hoặc lowest",
                                Path = "sort",
                                Location = "query"
                            }
                        }
                    });
                }

                // Gọi service để lấy reviews
                var (success, message, data) = await _movieService.GetMovieReviewsAsync(movieId, page, limit, sort);

                if (!success)
                {
                    return NotFound(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movie"] = new ValidationError
                            {
                                Msg = message,
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<GetMovieReviewsResponse>
                {
                    Message = message,
                    Result = data!
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình lấy danh sách review."
                });
            }
        }

        /// <summary>
        /// Get current user's review for a movie (Private - authentication required)
        /// </summary>
        /// <remarks>
        /// Retrieve the authenticated user's review for a specific movie. Returns null in the review field if the user has not reviewed this movie yet.
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        [HttpGet("{movieId}/my-review")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<GetMyReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyReview(int movieId)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ValidationErrorResponse
                    {
                        Message = "Xác thực thất bại",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["auth"] = new ValidationError
                            {
                                Msg = "Không thể xác định người dùng từ token",
                                Path = "token",
                                Location = "header"
                            }
                        }
                    });
                }

                // Validate movieId
                if (movieId <= 0)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movieId"] = new ValidationError
                            {
                                Msg = "Movie ID phải là số nguyên dương",
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Gọi service để lấy review của user
                var (success, message, data) = await _movieService.GetMyReviewAsync(movieId, userId);

                if (!success)
                {
                    return NotFound(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movie"] = new ValidationError
                            {
                                Msg = message,
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<GetMyReviewResponse>
                {
                    Message = message,
                    Result = data!
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình lấy review của bạn."
                });
            }
        }

        /// <summary>
        /// Get rating summary/statistics for a movie (Public - no authentication required)
        /// </summary>
        /// <remarks>
        /// Retrieve rating summary including average rating, total ratings count, and breakdown by star rating (1-5). Only includes active (non-deleted) reviews.
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        [HttpGet("{movieId}/rating-summary")]
        [ProducesResponseType(typeof(SuccessResponse<GetMovieRatingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieRatingSummary(int movieId)
        {
            try
            {
                // Validate movieId
                if (movieId <= 0)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực dữ liệu",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movieId"] = new ValidationError
                            {
                                Msg = "Movie ID phải là số nguyên dương",
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Gọi service để lấy rating summary
                var (success, message, data) = await _movieService.GetMovieRatingSummaryAsync(movieId);

                if (!success)
                {
                    return NotFound(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["movie"] = new ValidationError
                            {
                                Msg = message,
                                Path = "movieId",
                                Location = "path"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<GetMovieRatingSummaryResponse>
                {
                    Message = message,
                    Result = data!
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình lấy thống kê rating."
                });
            }
        }

        /// <summary>
        /// Delete (soft delete) a review for a movie
        /// </summary>
        /// <remarks>
        /// User must be authenticated and must have previously reviewed this movie. This is a soft delete - the review is marked as deleted but not removed from database.
        /// </remarks>
        /// <param name="movieId">Movie ID</param>
        [HttpDelete("{movieId}/reviews")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReview(int movieId)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ValidationErrorResponse
                    {
                        Message = "Xác thực thất bại",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["auth"] = new ValidationError
                            {
                                Msg = "Không thể xác định người dùng từ token",
                                Path = "token",
                                Location = "header"
                            }
                        }
                    });
                }

                // Gọi service để delete review
                var (success, message) = await _movieService.DeleteReviewAsync(movieId, userId);

                if (!success)
                {
                    // Xác định loại lỗi dựa trên message
                    if (message.Contains("Không tìm thấy phim") || message.Contains("chưa review"))
                    {
                        return NotFound(new ValidationErrorResponse
                        {
                            Message = message,
                            Errors = new Dictionary<string, ValidationError>
                            {
                                ["review"] = new ValidationError
                                {
                                    Msg = message,
                                    Path = "movieId",
                                    Location = "path"
                                }
                            }
                        });
                    }

                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = message,
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["error"] = new ValidationError
                            {
                                Msg = message,
                                Path = "request",
                                Location = "body"
                            }
                        }
                    });
                }

                // Trả về response thành công
                var response = new SuccessResponse<object>
                {
                    Message = message,
                    Result = new { }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình xóa review."
                });
            }
        }

    }

}
