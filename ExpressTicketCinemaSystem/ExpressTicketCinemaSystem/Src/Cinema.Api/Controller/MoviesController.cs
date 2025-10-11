using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controller
{
    [ApiController]
    [Route("cinema/movies")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovieResponse>>> GetAllMovies()
        {
            var movies = await _movieService.GetAllMoviesAsync();
            return Ok(movies);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<MovieResponse>>> SearchMovies(
            [FromQuery] string? title,
            [FromQuery] string? genre,
            [FromQuery] int? year,
            [FromQuery] string? actorName)
        {
            var result = await _movieService.SearchMoviesAsync(title, genre, year, actorName);
            return Ok(result);
        }

        [HttpGet("categories/now-showing")]
        public async Task<IActionResult> GetNowShowing(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery(Name = "sort_by")] string sortBy = "premiereDate",
            [FromQuery(Name = "sort_order")] string sortOrder = "asc")
        {
            var result = await _movieService.GetNowShowingMoviesAsync(page, limit, sortBy, sortOrder);

            return Ok(new
            {
                message = "Get now showing movies success",
                result = result
            });
        }


        [HttpGet("categories/coming-soon")]
        public async Task<IActionResult> GetComingSoon(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery(Name = "sort_by")] string sortBy = "premiereDate",
            [FromQuery(Name = "sort_order")] string sortOrder = "asc")
        {
            var result = await _movieService.GetComingSoonMoviesAsync(page, limit, sortBy, sortOrder);

            return Ok(new
            {
                message = "Get coming soon movies success",
                result = result
            });
        }


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


        [HttpGet("genre/{genre}")]
        public async Task<IActionResult> GetMoviesByGenre(
            [FromRoute] string genre,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery(Name = "sort_by")] string sortBy = "title",
            [FromQuery(Name = "sort_order")] string sortOrder = "asc")
        {
            var result = await _movieService.GetMoviesByGenreAsync(genre, page, limit, sortBy, sortOrder);

            return Ok(new
            {
                message = $"Get {genre} movies success",
                result = result
            });
        }


        [HttpGet("meta/stats")]
        public async Task<ActionResult<MovieStatisticsResponse>> GetMovieStats()
        {
            var stats = await _movieService.GetMovieStatisticsAsync();
            return Ok(stats);
        }

        [HttpGet("categories/top-rated")]
        public async Task<IActionResult> GetTopRatedMovies(
            [FromQuery] int limit = 10,
            [FromQuery] int min_ratings_count = 1,
            [FromQuery] string time_period = "all")
        {
            var result = await _movieService.GetTopRatedMoviesAsync(limit, min_ratings_count, time_period);
            return Ok(new
            {
                message = "Get top rated movies success",
                result
            });
        }



    }

}
