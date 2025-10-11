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
                Description = m.Description,
                TrailerUrl =m.TrailerUrl,
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
                query = query.Where(m => m.PremiereDate.Year == year.Value);
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

        public async Task<MoviePaginatedResponse> GetNowShowingMoviesAsync(int page, int limit, string sortBy, string sortOrder)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Where(m =>
                    m.IsActive &&
                    m.PremiereDate <= today &&
                    m.EndDate >= today
                );

            
            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("premieredate", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premieredate", "asc") => query.OrderBy(m => m.PremiereDate),
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                _ => query.OrderBy(m => m.PremiereDate)
            };

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var movies = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var movieResponses = movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Description = m.Description,
                DurationMinutes = m.DurationMinutes,
                Genre = string.Join(", ",
                   m.Genre?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(g => g.Trim()) ?? Enumerable.Empty<string>()),

                Language = m.Language,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                TrailerUrl = m.TrailerUrl,
                
                Country =m.Country,
                Production =m.Production,
                IsActive = m.IsActive,
                Status = "now_showing",
                Actor = m.MovieActors.Select(ma => new ActorDto
                {
                    Id = ma.Actor.ActorId,
                    Name = ma.Actor.Name,
                    ProfileImage = ma.Actor.AvatarUrl
                }).ToList()
            }).ToList();

            return new MoviePaginatedResponse
            {
                Movies = movieResponses,
                Total = total,
                Page = page,
                Limit = limit,
                TotalPages = totalPages
            };
        }



        public async Task<MoviePaginatedResponse> GetComingSoonMoviesAsync(int page, int limit, string sortBy, string sortOrder)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var sevenDaysFromNow = today.AddDays(7);

            var query = _context.Movies
            .Include(m => m.MovieActors)
            .ThenInclude(ma => ma.Actor)
            .Where(m =>
                m.IsActive &&
                m.PremiereDate > today &&
                m.PremiereDate <= sevenDaysFromNow 
       );


            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("premieredate", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premieredate", "asc") => query.OrderBy(m => m.PremiereDate),
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                _ => query.OrderBy(m => m.PremiereDate)
            };

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var movies = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var movieResponses = movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Description = m.Description,
                DurationMinutes = m.DurationMinutes,
                Genre = string.Join(", ",
                   m.Genre?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(g => g.Trim()) ?? Enumerable.Empty<string>()),
                Language = m.Language,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                TrailerUrl = m.TrailerUrl,
                IsActive = m.IsActive,
                Status = "coming_soon", 
                Actor = m.MovieActors.Select(ma => new ActorDto
                {
                    Id = ma.Actor.ActorId,
                    Name = ma.Actor.Name,
                    ProfileImage = ma.Actor.AvatarUrl
                }).ToList()
            }).ToList();

            return new MoviePaginatedResponse
            {
                Movies = movieResponses,
                Total = total,
                Page = page,
                Limit = limit,
                TotalPages = totalPages
            };
        }


        public async Task<IEnumerable<GenreCountDto>> GetAvailableGenresAsync()
        {
            var genres = await _context.Movies
                .Where(m => m.Genre != null)
                .Select(m => m.Genre)
                .ToListAsync();

            var genreCounts = genres
                .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(g => g.Trim())
                .GroupBy(g => g)
                .Select(g => new GenreCountDto
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderBy(g => g.Name);

            return genreCounts;
        }


        public async Task<IEnumerable<LanguageCountDto>> GetAvailableLanguagesAsync()
        {
            var languages = await _context.Movies
                .Where(m => m.Language != null)
                .Select(m => m.Language)
                .ToListAsync();

            var languageCounts = languages
                .SelectMany(l => l.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(l => l.Trim())
                .GroupBy(l => l)
                .Select(g => new LanguageCountDto
                {
                    Language = g.Key,
                    Count = g.Count()
                })
                .OrderBy(l => l.Language);

            return languageCounts;
        }


        public async Task<MoviePaginatedByGenreResponse> GetMoviesByGenreAsync(string genre, int page, int limit, string sortBy, string sortOrder)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var sevenDaysFromNow = today.AddDays(7);

            var moviesRaw = await _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Where(m => m.IsActive && !string.IsNullOrEmpty(m.Genre))
                .ToListAsync(); 

            
            var filtered = moviesRaw.Where(m =>
                m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Any(g => g.Trim().Equals(genre, StringComparison.OrdinalIgnoreCase)));

            
            filtered = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("title", "desc") => filtered.OrderByDescending(m => m.Title),
                ("title", "asc") => filtered.OrderBy(m => m.Title),
                ("premieredate", "desc") => filtered.OrderByDescending(m => m.PremiereDate),
                ("premieredate", "asc") => filtered.OrderBy(m => m.PremiereDate),
                _ => filtered.OrderBy(m => m.Title)
            };

            var total = filtered.Count();
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var movies = filtered
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            var movieResponses = movies.Select(m => new MovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Description = m.Description,
                DurationMinutes = m.DurationMinutes,
                Genre = string.Join(", ",
                   m.Genre?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(g => g.Trim()) ?? Enumerable.Empty<string>()),
                Language = m.Language,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                TrailerUrl = m.TrailerUrl,
                Country =m.Country,
                Production =m.Production,
                IsActive = m.IsActive,
                Status = GetMovieStatus(m.PremiereDate, m.EndDate),
                Actor = m.MovieActors.Select(ma => new ActorDto
                {
                    Id = ma.Actor.ActorId,
                    Name = ma.Actor.Name,
                    ProfileImage = ma.Actor.AvatarUrl
                }).ToList()
            }).ToList();
            
            return new MoviePaginatedByGenreResponse
            {
                Movies = movieResponses,
                Total = total,
                Page = page,
                Limit = limit,
                TotalPages = totalPages,
                Genre = genre
            };
        }



        public async Task<MovieStatisticsResponse> GetMovieStatisticsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
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

        public async Task<IEnumerable<TopRatedMovieResponse>> GetTopRatedMoviesAsync(int limit = 10, int minRatingsCount = 1, string timePeriod = "all")
        {
            var today = DateTime.Today;
            DateTime startDate = timePeriod.ToLower() switch
            {
                "week" => today.AddDays(-7),
                "month" => today.AddMonths(-1),
                "year" => today.AddYears(-1),
                _ => DateTime.MinValue 
            };

            var query = _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Include(m => m.RatingFilms)
                .Where(m => m.IsActive && m.RatingFilms.Count >= minRatingsCount);

            if (startDate != DateTime.MinValue)
            {
                query = query.Where(m => m.RatingFilms.Any(r => r.RatingAt >= startDate));
            }

            var result = await query
                .Select(m => new
                {
                    m.MovieId,
                    m.Title,
                    m.Description,
                    m.DurationMinutes,
                    m.Genre,
                    m.Language,
                    m.Director,
                    m.PremiereDate,
                    m.EndDate,
                    m.PosterUrl,
                    m.TrailerUrl,
                    Actors = m.MovieActors.Select(ma => new ActorDto
                    {
                        Id = ma.Actor.ActorId,
                        Name = ma.Actor.Name,
                        ProfileImage = ma.Actor.AvatarUrl
                    }).ToList(),
                    AverageRating = m.RatingFilms.Average(r => r.RatingStar),
                    TotalRatings = m.RatingFilms.Count()
                })
                .OrderByDescending(m => m.AverageRating)
                .ThenByDescending(m => m.TotalRatings)
                .Take(limit)
                .ToListAsync();

            return result.Select(m => new TopRatedMovieResponse
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                PosterUrl = m.PosterUrl,
                PremiereDate = m.PremiereDate.ToDateTime(TimeOnly.MinValue),
                EndDate = m.EndDate.ToDateTime(TimeOnly.MinValue),
                AverageRating = Math.Round(m.AverageRating, 2),
                Status = GetMovieStatus(m.PremiereDate, m.EndDate),
                TotalRatings = m.TotalRatings
            });

        }

        private string GetMovieStatus(DateOnly premiereDate, DateOnly endDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var daysUntilPremiere = (premiereDate.DayNumber - today.DayNumber);

            if (daysUntilPremiere >= 1 && daysUntilPremiere <= 7)
                return "coming_soon";

            if (premiereDate <= today && endDate >= today)
                return "now_showing";

            if (endDate < today)
                return "end";

            return "upcoming"; 
        }



    }

}
