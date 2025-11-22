using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using static ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests.CreateMovieRequest;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class MovieService : IMovieService
    {
        const int PRESALE_DAYS = 14;
        private readonly CinemaDbCoreContext _context;

        public MovieService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<MoviePaginatedResponse> GetAllMoviesAsync(
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
    double? minRating = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Where(m => m.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = status.ToLower() switch
                {
                    "now_showing" => query.Where(m =>
    m.EndDate >= today &&
    m.PremiereDate <= today.AddDays(PRESALE_DAYS)),

                    "coming_soon" => query.Where(m =>
    m.PremiereDate > today.AddDays(PRESALE_DAYS)),

                    "ended" => query.Where(m => m.EndDate < today),

                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m =>
                    m.Genre != null &&
                    m.Genre.ToLower().Contains(genre.ToLower()));
            }

            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(m =>
                    m.Language != null &&
                    m.Language.ToLower().Contains(language.ToLower()));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    m.Title.Contains(search) ||
                    (m.Description != null && m.Description.Contains(search)) ||
                    (m.Director != null && m.Director.Contains(search)) ||
                    m.MovieActors.Any(ma => ma.Actor.Name.Contains(search))
                );
            }

            if (premiereDateFrom.HasValue)
            {
                query = query.Where(m => m.PremiereDate >= premiereDateFrom.Value);
            }

            if (premiereDateTo.HasValue)
            {
                query = query.Where(m => m.PremiereDate <= premiereDateTo.Value);
            }

            if (minRating.HasValue)
            {
                decimal minRatingDecimal = (decimal)minRating.Value;
                query = query.Where(m => m.AverageRating >= minRatingDecimal);
            }

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("premiere_date", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premiere_date", "asc") => query.OrderBy(m => m.PremiereDate),
                ("average_rating", "desc") => query.OrderByDescending(m => m.AverageRating),
                ("average_rating", "asc") => query.OrderBy(m => m.AverageRating),
                ("ratings_count", "desc") => query.OrderByDescending(m => m.RatingsCount),
                ("ratings_count", "asc") => query.OrderBy(m => m.RatingsCount),
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                ("created_at", "desc") => query.OrderByDescending(m => m.CreatedAt),
                ("created_at", "asc") => query.OrderBy(m => m.CreatedAt),
                _ => query.OrderByDescending(m => m.PremiereDate) 
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
                Genre = m.Genre,
                Language = m.Language,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                BannerUrl = m.BannerUrl,
                TrailerUrl = m.TrailerUrl,
                Country = m.Country,
                Production = m.Production,
                IsActive = m.IsActive,
                Status = GetMovieStatus(m.PremiereDate, m.EndDate),

                AverageRating = (double?)m.AverageRating,
                RatingsCount = m.RatingsCount,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdateAt = m.UpdatedAt, 

                Actor = m.MovieActors.Select(ma => new ActorResponse
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
        public async Task<MoviePaginatedResponse> SearchMoviesAsync(
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
    string sortOrder = "asc")
        {
            var query = _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    m.Title.Contains(search) ||
                    (m.Description != null && m.Description.Contains(search)) ||
                    (m.Director != null && m.Director.Contains(search)) ||
                    m.MovieActors.Any(ma => ma.Actor.Name.Contains(search))
                );
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m =>
                    m.Genre != null && m.Genre.ToLower().Contains(genre.ToLower()));
            }

            if (year.HasValue)
            {
                query = query.Where(m => m.PremiereDate.Year == year.Value);
            }

            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(m =>
                    m.Language != null && m.Language.ToLower().Contains(language.ToLower()));
            }

            if (ratingMin.HasValue)
            {
                decimal ratingMinDecimal = (decimal)ratingMin.Value;
                query = query.Where(m => m.AverageRating >= ratingMinDecimal);
            }
            if (ratingMax.HasValue)
            {
                decimal ratingMaxDecimal = (decimal)ratingMax.Value;
                query = query.Where(m => m.AverageRating <= ratingMaxDecimal);
            }

            if (durationMin.HasValue)
            {
                query = query.Where(m => m.DurationMinutes >= durationMin.Value);
            }
            if (durationMax.HasValue)
            {
                query = query.Where(m => m.DurationMinutes <= durationMax.Value);
            }

            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(m =>
                    m.Country != null && m.Country.ToLower().Contains(country.ToLower()));
            }

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                ("premiere_date", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premiere_date", "asc") => query.OrderBy(m => m.PremiereDate),
                ("average_rating", "desc") => query.OrderByDescending(m => m.AverageRating),
                ("average_rating", "asc") => query.OrderBy(m => m.AverageRating),
                ("duration_minutes", "desc") => query.OrderByDescending(m => m.DurationMinutes),
                ("duration_minutes", "asc") => query.OrderBy(m => m.DurationMinutes),
                _ => query.OrderBy(m => m.Title) // default
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
                Genre = m.Genre,
                Language = m.Language,
                PremiereDate = m.PremiereDate,
                EndDate = m.EndDate,
                Director = m.Director,
                PosterUrl = m.PosterUrl,
                BannerUrl = m.BannerUrl,
                TrailerUrl = m.TrailerUrl,
                Country = m.Country,
                Production = m.Production,
                Status = GetMovieStatus(m.PremiereDate, m.EndDate),
                AverageRating = (double?)m.AverageRating,
                RatingsCount = m.RatingsCount,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdateAt = m.UpdatedAt,
                Actor = m.MovieActors.Select(ma => new ActorResponse
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

        public async Task<MoviePaginatedResponse> GetNowShowingMoviesAsync(
    int page = 1,
    int limit = 10,
    string? genre = null,
    string? language = null,
    double? ratingMin = null,
    int? durationMin = null,
    int? durationMax = null,
    string sortBy = "premiere_date",
    string sortOrder = "desc")
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Where(m =>
                    m.IsActive &&
                    m.EndDate >= today &&
    m.PremiereDate <= today.AddDays(PRESALE_DAYS)
                );

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(m =>
                    m.Genre != null &&
                    m.Genre.ToLower().Contains(genre.ToLower()));
            }

            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(m =>
                    m.Language != null &&
                    m.Language.ToLower().Contains(language.ToLower()));
            }

            if (ratingMin.HasValue)
            {
                decimal ratingMinDecimal = (decimal)ratingMin.Value;
                query = query.Where(m => m.AverageRating >= ratingMinDecimal);
            }

            if (durationMin.HasValue)
            {
                query = query.Where(m => m.DurationMinutes >= durationMin.Value);
            }
            if (durationMax.HasValue)
            {
                query = query.Where(m => m.DurationMinutes <= durationMax.Value);
            }

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("premiere_date", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premiere_date", "asc") => query.OrderBy(m => m.PremiereDate),
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                ("average_rating", "desc") => query.OrderByDescending(m => m.AverageRating),
                ("average_rating", "asc") => query.OrderBy(m => m.AverageRating),
                ("duration_minutes", "desc") => query.OrderByDescending(m => m.DurationMinutes),
                ("duration_minutes", "asc") => query.OrderBy(m => m.DurationMinutes),
                ("ratings_count", "desc") => query.OrderByDescending(m => m.RatingsCount),
                ("ratings_count", "asc") => query.OrderBy(m => m.RatingsCount),
                _ => query.OrderByDescending(m => m.PremiereDate)
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
                BannerUrl = m.BannerUrl,
                TrailerUrl = m.TrailerUrl,
                Country = m.Country,
                Production = m.Production,
                IsActive = m.IsActive,
                Status = "now_showing",
                AverageRating = (double?)m.AverageRating,
                RatingsCount = m.RatingsCount,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdateAt = m.UpdatedAt,
                Actor = m.MovieActors.Select(ma => new ActorResponse
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

        public async Task<MoviePaginatedResponse> GetComingSoonMoviesAsync(
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
    string sortOrder = "asc")
        {
            const int PRESALE_DAYS = 14; // cửa sổ “được coi như đang chiếu” trước ngày công chiếu
            var today = DateOnly.FromDateTime(DateTime.Today);
            var cutoff = today.AddDays(PRESALE_DAYS);

            // Coming soon: chỉ những phim công chiếu SAU cutoff (tức > 14 ngày nữa)
            var query = _context.Movies
                .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
                .Where(m => m.IsActive && m.PremiereDate > cutoff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(genre))
            {
                var g = genre.ToLowerInvariant();
                query = query.Where(m => m.Genre != null && m.Genre.ToLower().Contains(g));
            }

            if (!string.IsNullOrEmpty(language))
            {
                var lang = language.ToLowerInvariant();
                query = query.Where(m => m.Language != null && m.Language.ToLower().Contains(lang));
            }

            if (ratingMin.HasValue)
            {
                decimal minDec = (decimal)ratingMin.Value;
                query = query.Where(m => m.AverageRating >= minDec);
            }

            if (durationMin.HasValue) query = query.Where(m => m.DurationMinutes >= durationMin.Value);
            if (durationMax.HasValue) query = query.Where(m => m.DurationMinutes <= durationMax.Value);

            if (premiereDateFrom.HasValue) query = query.Where(m => m.PremiereDate >= premiereDateFrom.Value);
            if (premiereDateTo.HasValue) query = query.Where(m => m.PremiereDate <= premiereDateTo.Value);

            query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
            {
                ("premiere_date", "desc") => query.OrderByDescending(m => m.PremiereDate),
                ("premiere_date", "asc") => query.OrderBy(m => m.PremiereDate),
                ("title", "desc") => query.OrderByDescending(m => m.Title),
                ("title", "asc") => query.OrderBy(m => m.Title),
                ("average_rating", "desc") => query.OrderByDescending(m => m.AverageRating),
                ("average_rating", "asc") => query.OrderBy(m => m.AverageRating),
                ("duration_minutes", "desc") => query.OrderByDescending(m => m.DurationMinutes),
                ("duration_minutes", "asc") => query.OrderBy(m => m.DurationMinutes),
                ("ratings_count", "desc") => query.OrderByDescending(m => m.RatingsCount),
                ("ratings_count", "asc") => query.OrderBy(m => m.RatingsCount),
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
                BannerUrl = m.BannerUrl,
                TrailerUrl = m.TrailerUrl,
                Country = m.Country,
                Production = m.Production,
                IsActive = m.IsActive,
                Status = GetMovieStatus(m.PremiereDate, m.EndDate), // KHÔNG hardcode
                AverageRating = (double?)m.AverageRating,
                RatingsCount = m.RatingsCount,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdateAt = m.UpdatedAt,
                Actor = m.MovieActors.Select(ma => new ActorResponse
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


        public async Task<IEnumerable<GenreResponse>> GetAvailableGenresAsync()
        {
            var genres = await _context.Movies
                .Where(m => m.Genre != null)
                .Select(m => m.Genre)
                .ToListAsync();

            var genreCounts = genres
                .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(g => g.Trim())
                .GroupBy(g => g)
                .Select(g => new GenreResponse
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderBy(g => g.Name);

            return genreCounts;
        }


        public async Task<IEnumerable<LanguageCountResponse>> GetAvailableLanguagesAsync()
        {
            var languages = await _context.Movies
                .Where(m => m.Language != null)
                .Select(m => m.Language)
                .ToListAsync();

            var languageCounts = languages
                .SelectMany(l => l.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(l => l.Trim())
                .GroupBy(l => l)
                .Select(g => new LanguageCountResponse
                {
                    Language = g.Key,
                    Count = g.Count()
                })
                .OrderBy(l => l.Language);

            return languageCounts;
        }


        public async Task<MoviePaginatedByGenreResponse> GetMoviesByGenreAsync(
    string genre,
    int page = 1,
    int limit = 10,
    string sortBy = "title",
    string sortOrder = "asc")
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                throw new ArgumentException("Genre cannot be null or empty", nameof(genre));
            }

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

            if (!filtered.Any())
            {
                return new MoviePaginatedByGenreResponse
                {
                    Movies = new List<MovieResponse>(),
                    Total = 0,
                    Page = page,
                    Limit = limit,
                    TotalPages = 0,
                    Genre = genre
                };
            }

            filtered = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("title", "desc") => filtered.OrderByDescending(m => m.Title),
                ("title", "asc") => filtered.OrderBy(m => m.Title),
                ("premiere_date", "desc") => filtered.OrderByDescending(m => m.PremiereDate),
                ("premiere_date", "asc") => filtered.OrderBy(m => m.PremiereDate),
                ("average_rating", "desc") => filtered.OrderByDescending(m => m.AverageRating ?? 0),
                ("average_rating", "asc") => filtered.OrderBy(m => m.AverageRating ?? 0),
                ("duration_minutes", "desc") => filtered.OrderByDescending(m => m.DurationMinutes),
                ("duration_minutes", "asc") => filtered.OrderBy(m => m.DurationMinutes),
                ("ratings_count", "desc") => filtered.OrderByDescending(m => m.RatingsCount),
                ("ratings_count", "asc") => filtered.OrderBy(m => m.RatingsCount),
                ("created_at", "desc") => filtered.OrderByDescending(m => m.CreatedAt),
                ("created_at", "asc") => filtered.OrderBy(m => m.CreatedAt),
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
                BannerUrl = m.BannerUrl,
                TrailerUrl = m.TrailerUrl,
                Country = m.Country,
                Production = m.Production,
                IsActive = m.IsActive,
                Status = GetMovieStatus(m.PremiereDate, m.EndDate),
                AverageRating = (double?)m.AverageRating,
                RatingsCount = m.RatingsCount,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdateAt = m.UpdatedAt,
                Actor = m.MovieActors.Select(ma => new ActorResponse
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

            // Rating stats (only non-deleted ratings)
            var totalRatings = await _context.RatingFilms
                .Where(r => !r.IsDeleted)
                .CountAsync();
            var averageRating = totalRatings > 0
                ? await _context.RatingFilms
                    .Where(r => !r.IsDeleted)
                    .AverageAsync(r => r.RatingStar)
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
                .Where(m => m.IsActive && m.RatingFilms.Count(r => !r.IsDeleted) >= minRatingsCount);

            if (startDate != DateTime.MinValue)
            {
                query = query.Where(m => m.RatingFilms.Any(r => !r.IsDeleted && r.RatingAt >= startDate));
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
                    Actors = m.MovieActors.Select(ma => new ActorResponse
                    {
                        Id = ma.Actor.ActorId,
                        Name = ma.Actor.Name,
                        ProfileImage = ma.Actor.AvatarUrl
                    }).ToList(),
                    AverageRating = m.RatingFilms
                        .Where(r => !r.IsDeleted)
                        .Average(r => r.RatingStar),
                    TotalRatings = m.RatingFilms
                        .Count(r => !r.IsDeleted)
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
        public async Task<MovieResponse> GetMovieByIdAsync(int movieId)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);

            if (movie == null)
            {
                throw new KeyNotFoundException($"Movie with ID {movieId} not found");
            }

            return new MovieResponse
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Description = movie.Description,
                DurationMinutes = movie.DurationMinutes,
                Genre = movie.Genre,
                Language = movie.Language,
                PremiereDate = movie.PremiereDate,
                EndDate = movie.EndDate,
                Director = movie.Director,
                PosterUrl = movie.PosterUrl,
                BannerUrl = movie.BannerUrl,
                TrailerUrl = movie.TrailerUrl,
                Country = movie.Country,
                Production = movie.Production,
                IsActive = movie.IsActive,
                Status = GetMovieStatus(movie.PremiereDate, movie.EndDate),
                AverageRating = (double?)movie.AverageRating,
                RatingsCount = movie.RatingsCount,
                CreatedAt = movie.CreatedAt,
                CreatedBy = movie.CreatedBy,
                UpdateAt = movie.UpdatedAt,
                Actor = movie.MovieActors.Select(ma => new ActorResponse
                {
                    Id = ma.Actor.ActorId,
                    Name = ma.Actor.Name,
                    ProfileImage = ma.Actor.AvatarUrl
                }).ToList()
            };
        }
        private string GetMovieStatus(DateOnly premiereDate, DateOnly endDate)
        {
            const int PRESALE_DAYS = 14;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var daysUntilPremiere = premiereDate.DayNumber - today.DayNumber;

            // > 14 ngày nữa mới chiếu -> sắp chiếu (không cho tạo rạp)
            if (daysUntilPremiere > PRESALE_DAYS)
                return "coming_soon";

            // 1..14 ngày trước ngày chiếu -> cho phép coi như đang chiếu (mở bán/chuẩn bị)
            if (daysUntilPremiere >= 1 && daysUntilPremiere <= PRESALE_DAYS)
                return "now_showing";

            // Đúng/qua ngày chiếu và chưa hết hạn -> đang chiếu
            if (premiereDate <= today && endDate >= today)
                return "now_showing";

            // Hết hạn
            if (endDate < today)
                return "end";

            // Phòng hờ (hiếm khi rơi vào)
            return "upcoming";
        }

        public async Task<(bool success, string message, RatingFilm? rating)> CreateReviewAsync(
            int movieId, 
            int userId, 
            int ratingStar, 
            string comment,
            List<string>? imageUrls = null)
        {
            // 1. Kiểm tra phim tồn tại
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim", null);
            }

            // 2. Kiểm tra user đã review phim này chưa (chỉ xét review chưa bị xóa)
            var existingReview = await _context.RatingFilms
                .FirstOrDefaultAsync(r => r.MovieId == movieId && 
                                           r.UserId == userId &&
                                           !r.IsDeleted);
            
            if (existingReview != null)
            {
                return (false, "Bạn đã đánh giá phim này rồi", null);
            }

            // 3. Kiểm tra user đã mua vé và thanh toán thành công cho phim này chưa
            // Lấy customer_id từ user_id
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                return (false, "Bạn cần mua vé và thanh toán thành công để đánh giá phim này", null);
            }

            // Kiểm tra có booking nào đã paid cho phim này không
            var hasPaidTicket = await _context.Bookings
                .Where(b => b.CustomerId == customer.CustomerId && 
                           b.PaymentStatus == "PAID")
                .Join(_context.Tickets,
                    booking => booking.BookingId,
                    ticket => ticket.BookingId,
                    (booking, ticket) => ticket)
                .Join(_context.Showtimes,
                    ticket => ticket.ShowtimeId,
                    showtime => showtime.ShowtimeId,
                    (ticket, showtime) => showtime)
                .AnyAsync(showtime => showtime.MovieId == movieId);

            if (!hasPaidTicket)
            {
                return (false, "Bạn cần mua vé và thanh toán thành công để đánh giá phim này", null);
            }

            // 4. Validate image URLs (tối đa 3 ảnh)
            if (imageUrls != null && imageUrls.Count > 3)
            {
                return (false, "Tối đa 3 ảnh được phép", null);
            }

            // 5. Tạo review mới
            var newRating = new RatingFilm
            {
                MovieId = movieId,
                UserId = userId,
                RatingStar = ratingStar,
                Comment = comment,
                RatingAt = DateTime.UtcNow,
                ImageUrls = imageUrls != null && imageUrls.Any() ? string.Join(";", imageUrls) : null
            };

            _context.RatingFilms.Add(newRating);

            // 6. Cập nhật Movie (ratings_count và average_rating)
            var oldCount = movie.RatingsCount ?? 0;
            var oldAvg = movie.AverageRating ?? 0;

            movie.RatingsCount = oldCount + 1;
            movie.AverageRating = (oldAvg * oldCount + ratingStar) / (oldCount + 1);

            _context.Movies.Update(movie);

            // 7. Lưu thay đổi
            await _context.SaveChangesAsync();

            // 8. Load User để trả về user_name và user_avatar
            await _context.Entry(newRating).Reference(r => r.User).LoadAsync();

            return (true, "Đã tạo review thành công", newRating);
        }

        public async Task<(bool success, string message, RatingFilm? rating)> UpdateReviewAsync(
            int movieId,
            int userId,
            int ratingStar,
            string comment,
            List<string>? imageUrls = null)
        {
            // 1. Kiểm tra phim tồn tại
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim", null);
            }

            // 2. Tìm review hiện tại của user cho phim này (chỉ lấy review chưa xóa)
            var existingReview = await _context.RatingFilms
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId && !r.IsDeleted);

            if (existingReview == null)
            {
                return (false, "Bạn chưa review phim này", null);
            }

            // 3. Validate image URLs (tối đa 3 ảnh)
            if (imageUrls != null && imageUrls.Count > 3)
            {
                return (false, "Tối đa 3 ảnh được phép", null);
            }

            // 4. Lưu rating cũ để tính toán lại average
            var oldRatingStar = existingReview.RatingStar;

            // 5. Cập nhật review
            existingReview.RatingStar = ratingStar;
            existingReview.Comment = comment;
            existingReview.RatingAt = DateTime.UtcNow;
            existingReview.ImageUrls = imageUrls != null && imageUrls.Any() ? string.Join(";", imageUrls) : null;

            _context.RatingFilms.Update(existingReview);

            // 6. Recalculate Movie average_rating
            // Công thức: newAvg = (oldAvg * totalCount - oldStar + newStar) / totalCount
            var totalCount = movie.RatingsCount ?? 0;
            var oldAvg = movie.AverageRating ?? 0;

            if (totalCount > 0)
            {
                movie.AverageRating = (oldAvg * totalCount - oldRatingStar + ratingStar) / totalCount;
            }

            _context.Movies.Update(movie);

            // 7. Lưu thay đổi
            await _context.SaveChangesAsync();

            // 8. Load User để trả về user_name và user_avatar
            await _context.Entry(existingReview).Reference(r => r.User).LoadAsync();

            return (true, "Cập nhật review thành công", existingReview);
        }

        public async Task<(bool success, string message)> DeleteReviewAsync(
            int movieId,
            int userId)
        {
            // 1. Kiểm tra phim tồn tại
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim");
            }

            // 2. Tìm review hiện tại của user cho phim này (chưa bị xóa)
            var existingReview = await _context.RatingFilms
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId && !r.IsDeleted);

            if (existingReview == null)
            {
                return (false, "Bạn chưa review phim này");
            }

            // 3. Lưu rating star để tính toán lại average
            var deletedStar = existingReview.RatingStar;

            // 4. Soft delete review
            existingReview.IsDeleted = true;
            existingReview.DeletedAt = DateTime.UtcNow;

            _context.RatingFilms.Update(existingReview);

            // 5. Recalculate Movie average_rating và ratings_count
            // Công thức:
            // newTotalScore = oldAvg * totalCount - deletedStar
            // newCount = totalCount - 1
            // newAvg = newTotalScore / newCount (nếu newCount > 0)
            // newAvg = null (nếu newCount == 0)
            
            var totalCount = movie.RatingsCount ?? 0;
            var oldAvg = movie.AverageRating ?? 0;

            if (totalCount > 0)
            {
                var newTotalScore = oldAvg * totalCount - deletedStar;
                var newCount = totalCount - 1;

                if (newCount > 0)
                {
                    movie.AverageRating = newTotalScore / newCount;
                }
                else
                {
                    movie.AverageRating = null; // Hoặc 0, tuỳ yêu cầu
                }

                movie.RatingsCount = newCount;
            }

            _context.Movies.Update(movie);

            // 6. Lưu thay đổi
            await _context.SaveChangesAsync();

            return (true, "Đã xoá review của bạn cho phim này");
        }

        public async Task<(bool success, string message, GetMovieReviewsResponse? data)> GetMovieReviewsAsync(
            int movieId,
            int page = 1,
            int limit = 10,
            string sort = "newest")
        {
            // 1. Kiểm tra phim tồn tại
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim", null);
            }

            // 2. Đếm tổng số review (chỉ review chưa bị xóa)
            var totalReviews = await _context.RatingFilms
                .CountAsync(r => r.MovieId == movieId && !r.IsDeleted);

            // 3. Query reviews với pagination và sorting
            var query = _context.RatingFilms
                .Include(r => r.User)
                .Where(r => r.MovieId == movieId && !r.IsDeleted);

            // 4. Apply sorting
            query = sort.ToLower() switch
            {
                "oldest" => query.OrderBy(r => r.RatingAt),
                "highest" => query.OrderByDescending(r => r.RatingStar).ThenByDescending(r => r.RatingAt),
                "lowest" => query.OrderBy(r => r.RatingStar).ThenByDescending(r => r.RatingAt),
                _ => query.OrderByDescending(r => r.RatingAt) // "newest" is default
            };

            // 5. Apply pagination - Load data first
            var reviewsData = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(r => new
                {
                    r.RatingId,
                    r.UserId,
                    UserName = r.User.Fullname ?? "Ẩn danh",
                    UserAvatar = r.User.AvatarUrl,
                    r.RatingStar,
                    Comment = r.Comment ?? "",
                    r.RatingAt,
                    r.ImageUrls
                })
                .ToListAsync();

            // 6. Map to MovieReviewItem and process ImageUrls in memory
            var reviews = reviewsData.Select(r => new MovieReviewItem
            {
                RatingId = r.RatingId,
                UserId = r.UserId,
                UserName = r.UserName,
                UserAvatar = r.UserAvatar,
                RatingStar = r.RatingStar,
                Comment = r.Comment,
                RatingAt = r.RatingAt,
                ImageUrls = !string.IsNullOrEmpty(r.ImageUrls) 
                    ? r.ImageUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList() 
                    : new List<string>()
            }).ToList();

            // 7. Tạo response
            var response = new GetMovieReviewsResponse
            {
                MovieId = movieId,
                Page = page,
                Limit = limit,
                TotalReviews = totalReviews,
                AverageRating = movie.AverageRating,
                Items = reviews
            };

            return (true, "Lấy danh sách review thành công", response);
        }

        public async Task<(bool success, string message, GetMovieRatingSummaryResponse? data)> GetMovieRatingSummaryAsync(int movieId)
        {
            // 1. Kiểm tra phim tồn tại
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim", null);
            }

            // 2. Lấy breakdown - đếm số lượng rating theo từng sao (1-5), chỉ đếm review chưa xóa
            var breakdown = await _context.RatingFilms
                .Where(r => r.MovieId == movieId && !r.IsDeleted)
                .GroupBy(r => r.RatingStar)
                .Select(g => new { Star = g.Key, Count = g.Count() })
                .ToListAsync();

            // 3. Tạo dictionary breakdown với keys "1", "2", "3", "4", "5"
            var breakdownDict = new Dictionary<string, int>
            {
                ["5"] = 0,
                ["4"] = 0,
                ["3"] = 0,
                ["2"] = 0,
                ["1"] = 0
            };

            foreach (var item in breakdown)
            {
                breakdownDict[item.Star.ToString()] = item.Count;
            }

            // 4. Lấy total_ratings và average_rating từ Movie (đã được tính sẵn)
            var response = new GetMovieRatingSummaryResponse
            {
                MovieId = movieId,
                AverageRating = movie.AverageRating,
                TotalRatings = movie.RatingsCount ?? 0,
                Breakdown = breakdownDict
            };

            return (true, "Lấy thống kê rating thành công", response);
        }

        public async Task<(bool success, string message, GetMyReviewResponse? data)> GetMyReviewAsync(int movieId, int userId)
        {
            // 1. Kiểm tra phim tồn tại (optional theo yêu cầu)
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
            {
                return (false, "Không tìm thấy phim", null);
            }

            // 2. Tìm review của user cho phim này (chỉ lấy review chưa xóa) và include User
            var review = await _context.RatingFilms
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId && !r.IsDeleted);

            // 3. Tạo response
            var response = new GetMyReviewResponse
            {
                MovieId = movieId,
                UserId = userId,
                Review = review != null ? new MyReviewDetail
                {
                    RatingId = review.RatingId,
                    UserName = review.User.Fullname ?? "Ẩn danh",
                    UserAvatar = review.User.AvatarUrl,
                    RatingStar = review.RatingStar,
                    Comment = review.Comment ?? "",
                    RatingAt = review.RatingAt,
                    ImageUrls = !string.IsNullOrEmpty(review.ImageUrls) 
                        ? review.ImageUrls.Split(';').ToList() 
                        : new List<string>()
                } : null
            };

            return (true, "Lấy review của bạn thành công", response);
        }
    }

}
