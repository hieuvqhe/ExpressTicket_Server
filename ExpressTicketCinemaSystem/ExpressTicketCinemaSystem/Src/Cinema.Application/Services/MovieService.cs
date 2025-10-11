﻿using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
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
                        m.PremiereDate <= today &&
                        m.EndDate >= today), 

                    "coming_soon" => query.Where(m =>
                        m.PremiereDate > today &&
                        m.PremiereDate <= today.AddDays(7)), 

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
                    m.PremiereDate <= today &&
                    m.EndDate >= today
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

            if (premiereDateFrom.HasValue)
            {
                query = query.Where(m => m.PremiereDate >= premiereDateFrom.Value);
            }
            if (premiereDateTo.HasValue)
            {
                query = query.Where(m => m.PremiereDate <= premiereDateTo.Value);
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
                Country = m.Country,
                Production = m.Production,
                IsActive = m.IsActive,
                Status = "coming_soon",
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
                    Actors = m.MovieActors.Select(ma => new ActorResponse
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
