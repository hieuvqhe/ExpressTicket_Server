using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class MovieService : IMovieService
    {
        private readonly CinemaDbCoreContext _context;

        public MovieService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MovieResponse>> GetAllMoviesAsync()
        {
            var movies = await _context.Movies
       .Where(m => m.IsActive)
       .OrderBy(m => m.Title)
       .ToListAsync();

            return movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                Language = m.Language,
                Country = m.Country,
                IsActive = m.IsActive,
                PosterUrl = m.PosterUrl,
                Production = m.Production,
                Description = m.Description
            });
        }
        public async Task<IEnumerable<MovieResponse>> SearchMoviesAsync(string? title, string? genre, int? year, string? actorName)
        {
            var query = _context.Movies
       .Where(m => m.IsActive) 
       .Include(m => m.MovieActors)
       .ThenInclude(ma => ma.Actor)
       .AsQueryable();

            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(m => m.Title.Contains(title));
            }

            if (year.HasValue)
            {
                query = query.Where(m => m.PremiereDate.HasValue && m.PremiereDate.Value.Year == year.Value);
            }

            if (!string.IsNullOrEmpty(actorName))
            {
                query = query.Where(m =>
                    m.MovieActors.Any(ma => ma.Actor.Name.ToLower().Contains(actorName.ToLower()))
                );
            }

            var movies = await query.ToListAsync();

            if (!string.IsNullOrEmpty(genre))
            {
                movies = movies.Where(m =>
                    m.Genre != null &&
                    m.Genre.Split(',')
                        .Select(g => g.Trim().ToLower())
                        .Contains(genre.ToLower())
                ).ToList();
            }

            return movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                Language = m.Language,
                Country = m.Country,
                IsActive = m.IsActive,
                PosterUrl = m.PosterUrl,
                Production = m.Production,
                Description = m.Description
            });
        }

        public async Task<IEnumerable<MovieResponse>> GetNowShowingMoviesAsync()
        {
            var today = DateTime.Today;

            var movies = await _context.Movies
                .Where(m => m.PremiereDate <= today &&
                           m.EndDate >= today &&
                           m.IsActive) 
                .OrderBy(m => m.PremiereDate) 
                .ToListAsync();

            return movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                Language = m.Language,
                Country = m.Country,
                IsActive = m.IsActive,
                PosterUrl = m.PosterUrl,
                Production = m.Production,
                Description = m.Description
            });
        }

        public async Task<IEnumerable<MovieResponse>> GetComingSoonMoviesAsync()
        {
            var today = DateTime.Today;
            var sevenDaysFromNow = today.AddDays(7);

            var movies = await _context.Movies
                .Where(m => m.PremiereDate > today &&
                           m.PremiereDate <= sevenDaysFromNow &&
                           m.IsActive) 
                .OrderBy(m => m.PremiereDate) 
                .ToListAsync();

            return movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                Language = m.Language,
                Country = m.Country,
                IsActive = m.IsActive,
                PosterUrl = m.PosterUrl,
                Production = m.Production,
                Description = m.Description
            });
        }

        public async Task<IEnumerable<string>> GetAvailableGenresAsync()
        {
            var genres = await _context.Movies
                .Where(m => m.Genre != null)
                .Select(m => m.Genre)
                .ToListAsync();

            
            return genres
                .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(g => g.Trim())
                .Distinct()
                .OrderBy(g => g); 
        }

        public async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            return await _context.Movies
                .Where(m => m.Language != null)
                .Select(m => m.Language!.Trim())
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
        }

        public async Task<IEnumerable<MovieResponse>> GetMoviesByGenreAsync(string genre)
        {
            var movies = await _context.Movies
        .Where(m => m.Genre != null &&
                   m.Genre.Contains(genre) &&
                   m.IsActive)
        .OrderBy(m => m.Title)
        .ToListAsync();

            return movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                Language = m.Language,
                Country = m.Country,
                IsActive = m.IsActive,
                PosterUrl = m.PosterUrl,
                Production = m.Production,
                Description = m.Description
            });
        }

        public async Task<MovieStatisticsResponse> GetMovieStatisticsAsync()
        {
            var today = DateTime.Today;
            var sevenDaysFromNow = today.AddDays(7);

            var total = await _context.Movies.CountAsync();

            // Now Showing: PremiereDate <= today <= EndDate
            var nowShowing = await _context.Movies
                .CountAsync(m => m.PremiereDate <= today &&
                                m.EndDate >= today &&
                                m.IsActive);

            // Coming Soon: today < PremiereDate <= today + 7 days
            var comingSoon = await _context.Movies
                .CountAsync(m => m.PremiereDate > today &&
                                m.PremiereDate <= sevenDaysFromNow &&
                                m.IsActive);

            var active = await _context.Movies.CountAsync(m => m.IsActive);
            var inactive = await _context.Movies.CountAsync(m => !m.IsActive);

            // Rating stats
            var totalRatings = await _context.RatingFilms.CountAsync();
            var averageRating = totalRatings > 0
                ? await _context.RatingFilms.AverageAsync(r => r.RatingStar)
                : 0;

            return new MovieStatisticsResponse
            {
                TotalMovies = total,
                NowShowingMovies = nowShowing,
                ComingSoonMovies = comingSoon,
                ActiveMovies = active,
                InactiveMovies = inactive,
                TotalRatings = totalRatings,
                AverageRating = Math.Round(averageRating, 2)
            };
        }

        public async Task<IEnumerable<TopRatedMovieResponse>> GetTopRatedMoviesAsync(int top = 10)
        {
            var result = await _context.Movies
                .Where(m => m.IsActive &&  m.RatingFilms.Any()) // chỉ lấy phim có đánh giá
                .Select(m => new
                {
                    m.MovieId,
                    m.Title,
                    m.Genre,
                    m.PosterUrl,
                    m.PremiereDate,
                    m.EndDate,
                    AverageRating = m.RatingFilms.Average(r => r.RatingStar),
                    TotalRatings = m.RatingFilms.Count()
                })
                .OrderByDescending(m => m.AverageRating)
                .ThenByDescending(m => m.TotalRatings)
                .Take(top)
                .ToListAsync();

            return result.Select(m => new TopRatedMovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                PosterUrl = m.PosterUrl,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                AverageRating = Math.Round(m.AverageRating, 2),
                TotalRatings = m.TotalRatings
            });
        }

    }

}
