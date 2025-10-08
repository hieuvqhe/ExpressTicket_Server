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
        public async Task<ActionResult<IEnumerable<MovieResponse>>> GetNowShowing()
        {
            var result = await _movieService.GetNowShowingMoviesAsync();
            return Ok(result);
        }

        [HttpGet("categories/coming-soon")]
        public async Task<ActionResult<IEnumerable<MovieResponse>>> GetComingSoon()
        {
            var result = await _movieService.GetComingSoonMoviesAsync();
            return Ok(result);
        }

        [HttpGet("meta/genres")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableGenres()
        {
            var result = await _movieService.GetAvailableGenresAsync();
            return Ok(result);
        }

        [HttpGet("meta/languages")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableLanguages()
        {
            var result = await _movieService.GetAvailableLanguagesAsync();
            return Ok(result);
        }

        [HttpGet("genre/{genre}")]
        public async Task<ActionResult<IEnumerable<MovieResponse>>> GetMoviesByGenre(string genre)
        {
            var result = await _movieService.GetMoviesByGenreAsync(genre);
            return Ok(result);
        }

        [HttpGet("meta/stats")]
        public async Task<ActionResult<MovieStatisticsResponse>> GetMovieStats()
        {
            var stats = await _movieService.GetMovieStatisticsAsync();
            return Ok(stats);
        }

        [HttpGet("categories/top-rated")]
        public async Task<ActionResult<IEnumerable<TopRatedMovieResponse>>> GetTopRatedMovies()
        {
            var result = await _movieService.GetTopRatedMoviesAsync(); 
            return Ok(result);
        }


    }

}
