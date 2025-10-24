using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class MovieManagementService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IManagerService _managerService;

        public MovieManagementService(CinemaDbCoreContext context, IManagerService managerService)
        {
            _context = context;
            _managerService = managerService;
        }

        // ==================== ACTOR CRUD ====================

        public async Task<PaginatedActorsResponse> GetActorsAsync(
            int managerId,
            int page = 1,
            int limit = 10,
            string? search = null,
            string? sortBy = "name",
            string? sortOrder = "asc")
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);

            // ==================== BUSINESS LOGIC SECTION ====================
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _context.Actors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();
            query = ApplyActorSorting(query, sortBy, sortOrder);

            var actors = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new ActorListResponse
                {
                    Id = a.ActorId,
                    Name = a.Name,
                    ProfileImage = a.AvatarUrl
                })
                .ToListAsync();

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedActorsResponse
            {
                Actors = actors,
                Pagination = pagination
            };
        }

        public async Task<ActorResponse> GetActorByIdAsync(int actorId, int managerId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);

            var actor = await _context.Actors
                .FirstOrDefaultAsync(a => a.ActorId == actorId);

            if (actor == null)
                throw new NotFoundException("Không tìm thấy diễn viên với ID này.");

            // ==================== BUSINESS LOGIC SECTION ====================
            return new ActorResponse
            {
                Id = actor.ActorId,
                Name = actor.Name,
                ProfileImage = actor.AvatarUrl
            };
        }

        public async Task<ActorResponse> CreateActorAsync(int managerId, CreateActorRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);
            ValidateActorRequest(request);

            if (await _context.Actors.AnyAsync(a => a.Name.ToLower() == request.Name.Trim().ToLower()))
                throw new ConflictException("name", "Diễn viên với tên này đã tồn tại trong hệ thống");

            // ==================== BUSINESS LOGIC SECTION ====================
            var actor = new Actor
            {
                Name = request.Name.Trim(),
                AvatarUrl = request.AvatarUrl
            };

            _context.Actors.Add(actor);
            await _context.SaveChangesAsync();

            return new ActorResponse
            {
                Id = actor.ActorId,
                Name = actor.Name,
                ProfileImage = actor.AvatarUrl
            };
        }

        public async Task<ActorResponse> UpdateActorAsync(int actorId, int managerId, UpdateActorRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);

            var actor = await _context.Actors
                .FirstOrDefaultAsync(a => a.ActorId == actorId);

            if (actor == null)
                throw new NotFoundException("Không tìm thấy diễn viên với ID này.");

            ValidateActorRequest(request);

            if (await _context.Actors.AnyAsync(a =>
                a.ActorId != actorId && a.Name.ToLower() == request.Name.Trim().ToLower()))
                throw new ConflictException("name", "Diễn viên với tên này đã tồn tại trong hệ thống");

            // ==================== BUSINESS LOGIC SECTION ====================
            actor.Name = request.Name.Trim();
            actor.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();

            return new ActorResponse
            {
                Id = actor.ActorId,
                Name = actor.Name,
                ProfileImage = actor.AvatarUrl
            };
        }

        public async Task DeleteActorAsync(int actorId, int managerId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);

            var actor = await _context.Actors
                .FirstOrDefaultAsync(a => a.ActorId == actorId);

            if (actor == null)
                throw new NotFoundException("Không tìm thấy diễn viên với ID này.");

            var isUsedInMovies = await _context.MovieActors
                .AnyAsync(ma => ma.ActorId == actorId);

            if (isUsedInMovies)
                throw new ConflictException("actor", "Không thể xóa diễn viên đã được sử dụng trong phim");

            // ==================== BUSINESS LOGIC SECTION ====================
            _context.Actors.Remove(actor);
            await _context.SaveChangesAsync();
        }

        private async Task ValidateManagerExistsAsync(int managerId)
        {
            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["manager"] = new ValidationError
                    {
                        Msg = "Manager không tồn tại hoặc không có quyền",
                        Path = "managerId",
                        Location = "auth"
                    }
                });
            }
        }

        private void ValidateActorRequest(CreateActorRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên là bắt buộc",
                    Path = "name"
                };
            else if (request.Name.Trim().Length < 2)
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên phải có ít nhất 2 ký tự",
                    Path = "name"
                };
            else if (request.Name.Trim().Length > 100)
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên không được vượt quá 100 ký tự",
                    Path = "name"
                };

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
                !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out _))
            {
                errors["avatarUrl"] = new ValidationError
                {
                    Msg = "URL avatar không hợp lệ",
                    Path = "avatarUrl"
                };
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateActorRequest(UpdateActorRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên là bắt buộc",
                    Path = "name"
                };
            else if (request.Name.Trim().Length < 2)
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên phải có ít nhất 2 ký tự",
                    Path = "name"
                };
            else if (request.Name.Trim().Length > 100)
                errors["name"] = new ValidationError
                {
                    Msg = "Tên diễn viên không được vượt quá 100 ký tự",
                    Path = "name"
                };

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
                !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out _))
            {
                errors["avatarUrl"] = new ValidationError
                {
                    Msg = "URL avatar không hợp lệ",
                    Path = "avatarUrl"
                };
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private IQueryable<Actor> ApplyActorSorting(IQueryable<Actor> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "actor_id" => isAscending ? query.OrderBy(a => a.ActorId) : query.OrderByDescending(a => a.ActorId),
                "name" => isAscending ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
                _ => isAscending ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name)
            };
        }
        public async Task<MovieResponse> CreateMovieAsync(int managerId, CreateMovieRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);
            ValidateMovieRequest(request);
            await ValidateMovieUniqueAsync(request.Title, request.PremiereDate);
            ValidateMovieDates(request.PremiereDate, request.EndDate);

            // Validate actors
            if (request.ActorIds != null && request.ActorIds.Any())
            {
                await ValidateActorsExistAsync(request.ActorIds);
            }

            // ==================== VALIDATION: EXPERT RATING ====================
            if (request.AverageRating.HasValue)
            {
                if (request.AverageRating < 0 || request.AverageRating > 10)
                    throw new ValidationException("averageRating", "Điểm đánh giá chuyên gia phải từ 0 đến 10");

                if (request.RatingsCount.HasValue && request.RatingsCount < 0)
                    throw new ValidationException("ratingsCount", "Số lượng đánh giá không thể âm");
            }

            // ==================== BUSINESS LOGIC SECTION ====================

            // Tạo movie mới
            var movie = new Movie
            {
                Title = request.Title.Trim(),
                Genre = request.Genre,
                DurationMinutes = request.DurationMinutes,
                Director = request.Director.Trim(),
                Language = request.Language,
                Country = request.Country,
                IsActive = true,
                PosterUrl = request.PosterUrl,
                Production = request.Production,
                Description = request.Description,
                PremiereDate = request.PremiereDate,
                EndDate = request.EndDate,
                TrailerUrl = request.TrailerUrl,
                AverageRating = request.AverageRating, 
                RatingsCount = request.RatingsCount ?? 0, 
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = managerId.ToString(),
                ManagerId = managerId
            };

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            // Xử lý actors
            await ProcessMovieActorsAsync(movie.MovieId, request.ActorIds, request.NewActors, request.ActorRoles);

            return await GetMovieWithDetailsAsync(movie.MovieId);
        }

        public async Task<MovieResponse> UpdateMovieAsync(int movieId, int managerId, UpdateMovieRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateManagerExistsAsync(managerId);

            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null)
                throw new NotFoundException("Không tìm thấy phim với ID này.");

            // Security check - chỉ manager tạo phim mới được sửa
            if (movie.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền chỉnh sửa phim này",
                        Path = "movieId",
                        Location = "path"
                    }
                });
            }

            // ==================== NEW VALIDATION: CHECK SHOWTIMES ====================
            var hasShowtimes = await _context.Showtimes.AnyAsync(s => s.MovieId == movieId);
            if (hasShowtimes)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["movie"] = new ValidationError
                    {
                        Msg = "Không thể cập nhật phim đã có lịch chiếu",
                        Path = "movieId",
                        Location = "path"
                    }
                });
            }

            ValidateMovieRequest(request, isUpdate: true);

            // ==================== NEW VALIDATION: CHECK PREMIERE DATE ====================
            if (request.PremiereDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var currentPremiereDate = movie.PremiereDate;

                // Nếu premiere date cũ đã qua hoặc đang diễn ra
                if (currentPremiereDate <= today)
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["premiereDate"] = new ValidationError
                        {
                            Msg = "Không thể thay đổi ngày công chiếu cho phim đã/đang chiếu",
                            Path = "premiereDate",
                            Location = "body"
                        }
                    });
                }
            }

            if (request.PremiereDate.HasValue || request.EndDate.HasValue)
            {
                var premiereDate = request.PremiereDate ?? movie.PremiereDate;
                var endDate = request.EndDate ?? movie.EndDate;
                ValidateMovieDates(premiereDate, endDate);
            }

            // Validate actors
            if (request.ActorIds != null && request.ActorIds.Any())
            {
                await ValidateActorsExistAsync(request.ActorIds);
            }
            if (request.AverageRating.HasValue)
            {
                if (request.AverageRating < 0 || request.AverageRating > 10)
                    throw new ValidationException("averageRating", "Điểm đánh giá chuyên gia phải từ 0 đến 10");

                if (request.RatingsCount.HasValue && request.RatingsCount < 0)
                    throw new ValidationException("ratingsCount", "Số lượng đánh giá không thể âm");
            }

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật thông tin phim
            if (!string.IsNullOrWhiteSpace(request.Title))
                movie.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.Genre))
                movie.Genre = request.Genre;

            if (request.DurationMinutes.HasValue)
                movie.DurationMinutes = request.DurationMinutes.Value;

            if (!string.IsNullOrWhiteSpace(request.Director))
                movie.Director = request.Director.Trim();

            if (!string.IsNullOrWhiteSpace(request.Language))
                movie.Language = request.Language;

            if (!string.IsNullOrWhiteSpace(request.Country))
                movie.Country = request.Country;

            if (request.PosterUrl != null)
                movie.PosterUrl = request.PosterUrl;

            if (!string.IsNullOrWhiteSpace(request.Production))
                movie.Production = request.Production;

            if (request.Description != null)
                movie.Description = request.Description;

            if (request.PremiereDate.HasValue)
                movie.PremiereDate = request.PremiereDate.Value;

            if (request.EndDate.HasValue)
                movie.EndDate = request.EndDate.Value;

            if (request.TrailerUrl != null)
                movie.TrailerUrl = request.TrailerUrl;

            if (request.IsActive.HasValue)
                movie.IsActive = request.IsActive.Value;
            if (request.AverageRating.HasValue)
                movie.AverageRating = request.AverageRating.Value;

            if (request.RatingsCount.HasValue)
                movie.RatingsCount = request.RatingsCount.Value;

            movie.UpdatedAt = DateTime.UtcNow;

            // Xử lý actors nếu có
            if (request.ActorIds != null || request.NewActors != null)
            {
                // Xóa actors cũ
                var existingMovieActors = _context.MovieActors.Where(ma => ma.MovieId == movieId);
                _context.MovieActors.RemoveRange(existingMovieActors);

                // Thêm actors mới
                await ProcessMovieActorsAsync(movieId, request.ActorIds, request.NewActors, request.ActorRoles);
            }

            await _context.SaveChangesAsync();

            return await GetMovieWithDetailsAsync(movieId);
        }
        private async Task ProcessMovieActorsAsync(int movieId, List<int>? actorIds, List<CreateActorInMovieRequest>? newActors, Dictionary<int, string>? actorRoles)
        {
            var movieActors = new List<MovieActor>();

            // Xử lý actors có sẵn
            if (actorIds != null && actorIds.Any())
            {
                foreach (var actorId in actorIds)
                {
                    var role = actorRoles?.ContainsKey(actorId) == true ? actorRoles[actorId] : "Diễn viên";
                    movieActors.Add(new MovieActor
                    {
                        MovieId = movieId,
                        ActorId = actorId,
                        Role = role
                    });
                }
            }

            // Xử lý tạo actors mới
            if (newActors != null && newActors.Any())
            {
                foreach (var newActor in newActors)
                {
                    // Tạo actor mới
                    var actor = new Actor
                    {
                        Name = newActor.Name.Trim(),
                        AvatarUrl = newActor.AvatarUrl
                    };

                    _context.Actors.Add(actor);
                    await _context.SaveChangesAsync(); // Save để lấy ActorId

                    // Thêm vào movie actors
                    movieActors.Add(new MovieActor
                    {
                        MovieId = movieId,
                        ActorId = actor.ActorId,
                        Role = newActor.Role
                    });
                }
            }

            if (movieActors.Any())
            {
                _context.MovieActors.AddRange(movieActors);
                await _context.SaveChangesAsync();
            }
        }
        public async Task DeleteMovieAsync(int movieId, int managerId)
        {
            await ValidateManagerExistsAsync(managerId);

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null)
                throw new NotFoundException("Không tìm thấy phim với ID này.");

            // Security check
            if (movie.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền xóa phim này",
                        Path = "movieId",
                        Location = "path"
                    }
                });
            }

            var hasShowtimes = await _context.Showtimes.AnyAsync(s => s.MovieId == movieId);
            if (hasShowtimes)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["movie"] = new ValidationError
                    {
                        Msg = "Không thể xóa phim đã có lịch chiếu",
                        Path = "movieId",
                        Location = "path"
                    }
                });
            }

            movie.IsActive = false;
            movie.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        private void ValidateMovieRequest(CreateMovieRequest request, bool isUpdate = false)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (!isUpdate)
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    errors["title"] = new ValidationError { Msg = "Tiêu đề phim là bắt buộc", Path = "title" };

                if (string.IsNullOrWhiteSpace(request.Genre))
                    errors["genre"] = new ValidationError { Msg = "Thể loại phim là bắt buộc", Path = "genre" };

                if (string.IsNullOrWhiteSpace(request.Director))
                    errors["director"] = new ValidationError { Msg = "Đạo diễn là bắt buộc", Path = "director" };

                if (string.IsNullOrWhiteSpace(request.Language))
                    errors["language"] = new ValidationError { Msg = "Ngôn ngữ là bắt buộc", Path = "language" };

                if (string.IsNullOrWhiteSpace(request.Country))
                    errors["country"] = new ValidationError { Msg = "Quốc gia là bắt buộc", Path = "country" };

                if (string.IsNullOrWhiteSpace(request.Production))
                    errors["production"] = new ValidationError { Msg = "Nhà sản xuất là bắt buộc", Path = "production" };

                if (string.IsNullOrWhiteSpace(request.Description))
                    errors["description"] = new ValidationError { Msg = "Mô tả phim là bắt buộc", Path = "description" };
            }

            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim().Length > 200)
                errors["title"] = new ValidationError { Msg = "Tiêu đề phim không được vượt quá 200 ký tự", Path = "title" };

            if (!string.IsNullOrWhiteSpace(request.Director) && request.Director.Trim().Length > 100)
                errors["director"] = new ValidationError { Msg = "Tên đạo diễn không được vượt quá 100 ký tự", Path = "director" };

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 2000)
                errors["description"] = new ValidationError { Msg = "Mô tả phim không được vượt quá 2000 ký tự", Path = "description" };

            if (request.DurationMinutes < 1 || request.DurationMinutes > 500)
                errors["durationMinutes"] = new ValidationError { Msg = "Thời lượng phim phải từ 1 đến 500 phút", Path = "durationMinutes" };

            if (!string.IsNullOrWhiteSpace(request.PosterUrl) && !Uri.TryCreate(request.PosterUrl, UriKind.Absolute, out _))
                errors["posterUrl"] = new ValidationError { Msg = "URL poster không hợp lệ", Path = "posterUrl" };

            if (!string.IsNullOrWhiteSpace(request.TrailerUrl) && !Uri.TryCreate(request.TrailerUrl, UriKind.Absolute, out _))
                errors["trailerUrl"] = new ValidationError { Msg = "URL trailer không hợp lệ", Path = "trailerUrl" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateMovieRequest(UpdateMovieRequest request, bool isUpdate = true)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim().Length > 200)
                errors["title"] = new ValidationError { Msg = "Tiêu đề phim không được vượt quá 200 ký tự", Path = "title" };

            if (!string.IsNullOrWhiteSpace(request.Director) && request.Director.Trim().Length > 100)
                errors["director"] = new ValidationError { Msg = "Tên đạo diễn không được vượt quá 100 ký tự", Path = "director" };

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 2000)
                errors["description"] = new ValidationError { Msg = "Mô tả phim không được vượt quá 2000 ký tự", Path = "description" };

            if (request.DurationMinutes.HasValue && (request.DurationMinutes < 1 || request.DurationMinutes > 500))
                errors["durationMinutes"] = new ValidationError { Msg = "Thời lượng phim phải từ 1 đến 500 phút", Path = "durationMinutes" };

            if (!string.IsNullOrWhiteSpace(request.PosterUrl) && !Uri.TryCreate(request.PosterUrl, UriKind.Absolute, out _))
                errors["posterUrl"] = new ValidationError { Msg = "URL poster không hợp lệ", Path = "posterUrl" };

            if (!string.IsNullOrWhiteSpace(request.TrailerUrl) && !Uri.TryCreate(request.TrailerUrl, UriKind.Absolute, out _))
                errors["trailerUrl"] = new ValidationError { Msg = "URL trailer không hợp lệ", Path = "trailerUrl" };

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private void ValidateMovieDates(DateOnly premiereDate, DateOnly endDate)
        {
            var errors = new Dictionary<string, ValidationError>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (premiereDate < today)
                errors["premiereDate"] = new ValidationError { Msg = "Ngày công chiếu không thể trong quá khứ", Path = "premiereDate" };

            if (endDate <= premiereDate)
                errors["endDate"] = new ValidationError { Msg = "Ngày kết thúc phải sau ngày công chiếu", Path = "endDate" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateMovieUniqueAsync(string title, DateOnly premiereDate)
        {
            var existingMovie = await _context.Movies
                .FirstOrDefaultAsync(m =>
                    m.Title.ToLower() == title.Trim().ToLower() &&
                    m.PremiereDate == premiereDate);

            if (existingMovie != null)
                throw new ConflictException("movie", "Đã tồn tại phim với cùng tiêu đề và ngày công chiếu");
        }

        private async Task<MovieResponse> GetMovieWithDetailsAsync(int movieId)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieActors)
                    .ThenInclude(ma => ma.Actor)
                .Include(m => m.Manager) // THÊM INCLUDE MANAGER
                    .ThenInclude(mgr => mgr.User) // THÊM INCLUDE USER CỦA MANAGER
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null)
                throw new NotFoundException("Không tìm thấy phim.");

            // Tính toán status dựa trên ngày hiện tại
            var status = CalculateMovieStatus(movie.PremiereDate, movie.EndDate, movie.IsActive);

            // Lấy manager name thay vì ID
            var managerName = movie.Manager?.User?.Fullname ?? "Unknown Manager";

            return new MovieResponse
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Genre = movie.Genre,
                DurationMinutes = movie.DurationMinutes,
                Director = movie.Director,
                Language = movie.Language,
                Country = movie.Country,
                PosterUrl = movie.PosterUrl,
                Production = movie.Production,
                Description = movie.Description,
                PremiereDate = movie.PremiereDate,
                EndDate = movie.EndDate,
                TrailerUrl = movie.TrailerUrl,
                AverageRating = (double?)movie.AverageRating, // GIỮ NGUYÊN, CÓ THỂ NULL
                RatingsCount = movie.RatingsCount, // GIỮ NGUYÊN, CÓ THỂ NULL
                IsActive = movie.IsActive,
                Status = status,
                CreatedAt = movie.CreatedAt,
                CreatedBy = managerName, // SỬA: DÙNG MANAGER NAME THAY VÌ ID
                UpdateAt = movie.UpdatedAt,
                Actor = movie.MovieActors.Select(ma => new ActorResponse
                {
                    Id = ma.Actor.ActorId,
                    Name = ma.Actor.Name,
                    ProfileImage = ma.Actor.AvatarUrl
                }).ToList()
            };
        }
        private string CalculateMovieStatus(DateOnly premiereDate, DateOnly endDate, bool isActive)
        {
            if (!isActive)
                return "inactive";

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysUntilPremiere = (premiereDate.DayNumber - today.DayNumber);

            if (daysUntilPremiere >= 1 && daysUntilPremiere <= 7)
                return "coming_soon";

            if (premiereDate <= today && endDate >= today)
                return "now_showing";

            if (endDate < today)
                return "end";

            return "upcoming";
        }
        private async Task ValidateActorsExistAsync(List<int> actorIds)
        {
            var existingActors = await _context.Actors
                .Where(a => actorIds.Contains(a.ActorId))
                .Select(a => a.ActorId)
                .ToListAsync();

            var missingActors = actorIds.Except(existingActors).ToList();
            if (missingActors.Any())
            {
                throw new NotFoundException($"Không tìm thấy diễn viên với IDs: {string.Join(", ", missingActors)}");
            }
        }
    }
}