using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IShowtimeService
    {
        Task<PartnerShowtimeCreateResponse> CreatePartnerShowtimeAsync(int partnerId, PartnerShowtimeCreateRequest request, string auditAction = "PARTNER_CREATE_SHOWTIME");
        Task<PartnerShowtimeCreateResponse> UpdatePartnerShowtimeAsync(int partnerId, int showtimeId, PartnerShowtimeCreateRequest request, string auditAction = "PARTNER_UPDATE_SHOWTIME");
        Task<PartnerShowtimeCreateResponse> DeletePartnerShowtimeAsync(int partnerId, int showtimeId, string auditAction = "PARTNER_DELETE_SHOWTIME");
        Task<PartnerShowtimeDetailResponse> GetPartnerShowtimeByIdAsync(int partnerId, int showtimeId, int userId);
        Task<PartnerShowtimeDetailResponse> GetShowtimeByIdPublicAsync(int showtimeId);

        Task<PartnerShowtimeListResponse> GetPartnerShowtimesAsync(int partnerId, int userId, PartnerShowtimeQueryRequest request);
    }

    public class ShowtimeService : IShowtimeService
    {
        private const int PRESALE_DAYS = 14;
        private static DateTime NowVN() => DateTime.UtcNow.AddHours(7);
        private static DateOnly TodayVN() => DateOnly.FromDateTime(NowVN());
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmployeeCinemaAssignmentService _employeeCinemaAssignmentService;

        public ShowtimeService(CinemaDbCoreContext context, IAuditLogService auditLogService, IEmployeeCinemaAssignmentService employeeCinemaAssignmentService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _employeeCinemaAssignmentService = employeeCinemaAssignmentService;
        }

        public async Task<PartnerShowtimeCreateResponse> CreatePartnerShowtimeAsync(int partnerId, PartnerShowtimeCreateRequest request, string auditAction = "PARTNER_CREATE_SHOWTIME")
        {
            // ==================== VALIDATION SECTION ====================
            await ValidatePartnerOwnsCinemaAndScreenAsync(partnerId, request.CinemaId, request.ScreenId);
            await ValidateMovieCreatableWindowAsync(request.MovieId); // Yêu cầu 1
            ValidateShowtimeDateTime(request.StartTime, request.EndTime);
            await ValidateShowtimeDurationWithMovieAsync(request.MovieId, request.StartTime, request.EndTime); // Yêu cầu 2
            await ValidateAvailableSeatsWithinScreenCapacityAsync(request.ScreenId, request.AvailableSeats); // Yêu cầu 3
            await ValidateNoOverlappingShowtimeAsync(request.ScreenId, request.StartTime, request.EndTime);
            ValidateShowtimeStatus(request.Status);

            // ==================== BUSINESS LOGIC SECTION ====================
            var showtime = new Showtime
            {
                CinemaId = request.CinemaId,
                ScreenId = request.ScreenId,
                MovieId = request.MovieId,
                ShowDatetime = request.StartTime,
                EndTime = request.EndTime,
                BasePrice = request.BasePrice,
                AvailableSeats = request.AvailableSeats,
                FormatType = request.FormatType,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Showtimes.Add(showtime);
            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: auditAction,
                tableName: "Showtime",
                recordId: showtime.ShowtimeId,
                beforeData: null,
                afterData: BuildShowtimeSnapshot(showtime),
                metadata: new
                {
                    partnerId,
                    request.CinemaId,
                    request.ScreenId,
                    request.MovieId
                });

            return new PartnerShowtimeCreateResponse
            {
                ShowtimeId = showtime.ShowtimeId
            };
        }

        public async Task<PartnerShowtimeCreateResponse> UpdatePartnerShowtimeAsync(int partnerId, int showtimeId, PartnerShowtimeCreateRequest request, string auditAction = "PARTNER_UPDATE_SHOWTIME")
        {
            // ==================== VALIDATION SECTION ====================
            var existingShowtime = await ValidateAndGetShowtimeAsync(partnerId, showtimeId);

            await ValidatePartnerOwnsCinemaAndScreenAsync(partnerId, request.CinemaId, request.ScreenId);
            await ValidateMovieCreatableWindowAsync(request.MovieId); // Yêu cầu 1
            ValidateShowtimeDateTime(request.StartTime, request.EndTime);
            await ValidateShowtimeDurationWithMovieAsync(request.MovieId, request.StartTime, request.EndTime); // Yêu cầu 2
            await ValidateAvailableSeatsWithinScreenCapacityAsync(request.ScreenId, request.AvailableSeats); // Yêu cầu 3
            await ValidateNoOverlappingShowtimeAsync(request.ScreenId, request.StartTime, request.EndTime, showtimeId);
            ValidateShowtimeStatus(request.Status);

            // ==================== BUSINESS LOGIC SECTION ====================
            var beforeSnapshot = BuildShowtimeSnapshot(existingShowtime);

            existingShowtime.CinemaId = request.CinemaId;
            existingShowtime.ScreenId = request.ScreenId;
            existingShowtime.MovieId = request.MovieId;
            existingShowtime.ShowDatetime = request.StartTime;
            existingShowtime.EndTime = request.EndTime;
            existingShowtime.BasePrice = request.BasePrice;
            existingShowtime.AvailableSeats = request.AvailableSeats;
            existingShowtime.FormatType = request.FormatType;
            existingShowtime.Status = request.Status;
            existingShowtime.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: auditAction,
                tableName: "Showtime",
                recordId: existingShowtime.ShowtimeId,
                beforeData: beforeSnapshot,
                afterData: BuildShowtimeSnapshot(existingShowtime),
                metadata: new
                {
                    partnerId,
                    request.CinemaId,
                    request.ScreenId,
                    request.MovieId
                });

            return new PartnerShowtimeCreateResponse
            {
                ShowtimeId = existingShowtime.ShowtimeId
            };
        }

        public async Task<PartnerShowtimeCreateResponse> DeletePartnerShowtimeAsync(int partnerId, int showtimeId, string auditAction = "PARTNER_DELETE_SHOWTIME")
        {
            // ==================== VALIDATION SECTION ====================
            var existingShowtime = await ValidateAndGetShowtimeAsync(partnerId, showtimeId);

            // Kiểm tra xem showtime đã bị disabled chưa
            if (existingShowtime.Status == "disabled")
            {
                throw new ConflictException("showtime", "Suất chiếu đã bị vô hiệu hóa trước đó");
            }

            // Yêu cầu 4: Kiểm tra xem có booking nào cho showtime này không
            await ValidateNoBookingsForShowtimeAsync(showtimeId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var beforeSnapshot = BuildShowtimeSnapshot(existingShowtime);

            existingShowtime.Status = "disabled";
            existingShowtime.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: auditAction,
                tableName: "Showtime",
                recordId: existingShowtime.ShowtimeId,
                beforeData: beforeSnapshot,
                afterData: BuildShowtimeSnapshot(existingShowtime),
                metadata: new { partnerId });

            return new PartnerShowtimeCreateResponse
            {
                ShowtimeId = existingShowtime.ShowtimeId
            };
        }

        private static object BuildShowtimeSnapshot(Showtime showtime) => new
        {
            showtime.ShowtimeId,
            showtime.CinemaId,
            showtime.ScreenId,
            showtime.MovieId,
            showtime.ShowDatetime,
            showtime.EndTime,
            showtime.BasePrice,
            showtime.AvailableSeats,
            showtime.FormatType,
            showtime.Status,
            showtime.CreatedAt,
            showtime.UpdatedAt
        };

        // ==================== VALIDATION METHODS ====================

        // YÊU CẦU 1: Validate movie đang trong thời gian công chiếu
        private async Task ValidateMovieExistsAndInReleasePeriodAsync(int movieId)
        {
            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);

            if (movie == null)
            {
                throw new ValidationException("movie_id", "Phim không tồn tại hoặc không hoạt động");
            }

            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            if (currentDate < movie.PremiereDate)
            {
                throw new ValidationException("movie_id",
                    $"Phim chưa đến ngày công chiếu. Ngày công chiếu: {movie.PremiereDate:dd/MM/yyyy}");
            }

            if (currentDate > movie.EndDate)
            {
                throw new ValidationException("movie_id",
                    $"Phim đã kết thúc thời gian công chiếu. Ngày kết thúc: {movie.EndDate:dd/MM/yyyy}");
            }
        }

        // YÊU CẦU 2: Validate thời lượng showtime so với duration của movie
        private async Task ValidateShowtimeDurationWithMovieAsync(int movieId, DateTime startTime, DateTime endTime)
        {
            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null) return;

            var showtimeDuration = endTime - startTime;
            var movieDuration = TimeSpan.FromMinutes(movie.DurationMinutes);
            var maxAllowedDuration = movieDuration + TimeSpan.FromMinutes(30);
            var minAllowedDuration = movieDuration;

            if (showtimeDuration > maxAllowedDuration)
            {
                throw new ValidationException("end_time",
                    $"Thời lượng suất chiếu không được vượt quá thời lượng phim quá 30 phút. " +
                    $"Thời lượng phim: {movieDuration.TotalMinutes} phút, " +
                    $"Thời lượng tối đa cho phép: {maxAllowedDuration.TotalMinutes} phút, " +
                    $"Thời lượng hiện tại: {showtimeDuration.TotalMinutes:0} phút");
            }

            if (showtimeDuration < minAllowedDuration)
            {
                throw new ValidationException("end_time",
                    $"Thời lượng suất chiếu phải ít nhất bằng thời lượng phim. " +
                    $"Thời lượng phim: {movieDuration.TotalMinutes} phút, " +
                    $"Thời lượng hiện tại: {showtimeDuration.TotalMinutes:0} phút");
            }
        }

        // YÊU CẦU 3: Validate available_seats không vượt quá capacity của screen
        private async Task ValidateAvailableSeatsWithinScreenCapacityAsync(int screenId, int availableSeats)
        {
            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            if (screen == null) return;

            if (availableSeats > screen.Capacity)
            {
                throw new ValidationException("available_seats",
                    $"Số ghế có sẵn không được vượt quá sức chứa của phòng chiếu. " +
                    $"Sức chứa tối đa: {screen.Capacity}, " +
                    $"Số ghế hiện tại: {availableSeats}");
            }

            if (availableSeats <= 0)
            {
                throw new ValidationException("available_seats",
                    "Số ghế có sẵn phải lớn hơn 0");
            }
        }

        // YÊU CẦU 4: Validate không có booking nào cho showtime khi soft delete
        private async Task ValidateNoBookingsForShowtimeAsync(int showtimeId)
        {
            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.ShowtimeId == showtimeId && b.Status != "cancelled");

            if (hasBookings)
            {
                throw new ConflictException("showtime",
                    "Không thể xóa suất chiếu vì đã có người đặt vé. Chỉ có thể xóa các suất chiếu chưa có người đặt.");
            }
        }

        // CÁC VALIDATION METHODS CŨ GIỮ NGUYÊN
        private async Task ValidatePartnerOwnsCinemaAndScreenAsync(int partnerId, int cinemaId, int screenId)
        {
            var errors = new Dictionary<string, ValidationError>();

            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                errors["cinema_id"] = new ValidationError
                {
                    Msg = "Rạp chiếu không thuộc về partner của bạn hoặc không tồn tại",
                    Path = "cinema_id"
                };
            }

            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId && s.CinemaId == cinemaId);

            if (screen == null)
            {
                errors["screen_id"] = new ValidationError
                {
                    Msg = "Phòng chiếu không thuộc về rạp đã chọn hoặc không tồn tại",
                    Path = "screen_id"
                };
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private async Task ValidateNoOverlappingShowtimeAsync(int screenId, DateTime startTime, DateTime endTime, int excludeShowtimeId = 0)
        {
            var query = _context.Showtimes
                .Where(s => s.ScreenId == screenId
                         && s.Status != "disabled"
                         && ((s.ShowDatetime <= startTime && s.EndTime > startTime) ||
                             (s.ShowDatetime < endTime && s.EndTime >= endTime) ||
                             (s.ShowDatetime >= startTime && s.EndTime <= endTime)));

            if (excludeShowtimeId > 0)
            {
                query = query.Where(s => s.ShowtimeId != excludeShowtimeId);
            }

            var overlappingShowtime = await query.FirstOrDefaultAsync();

            if (overlappingShowtime != null)
            {
                throw new ConflictException("showtime",
                    $"Đã tồn tại suất chiếu khác trong khoảng thời gian này. Thời gian chiếu từ {overlappingShowtime.ShowDatetime:dd/MM/yyyy HH:mm} đến {overlappingShowtime.EndTime:dd/MM/yyyy HH:mm}");
            }
        }

        private void ValidateShowtimeStatus(string status)
        {
            var validStatuses = new[] { "scheduled", "finished", "disabled" };

            if (!validStatuses.Contains(status.ToLower()))
            {
                throw new ValidationException("status",
                    $"Trạng thái không hợp lệ. Trạng thái hợp lệ: {string.Join(", ", validStatuses)}");
            }
        }

        private async Task<Showtime> ValidateAndGetShowtimeAsync(int partnerId, int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu với ID đã cho");
            }

            if (showtime.Cinema.PartnerId != partnerId)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu với ID đã cho");
            }

            return showtime;
        }

        public async Task<PartnerShowtimeDetailResponse> GetPartnerShowtimeByIdAsync(int partnerId, int showtimeId, int userId)
        {
            // Nếu là Staff, kiểm tra có được phân quyền rạp của showtime này không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.UserType == "Staff" || user?.UserType == "Marketing" || user?.UserType == "Cashier")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee != null)
                {
                    // Lấy cinemaId từ showtime
                    var showtimeCinema = await _context.Showtimes
                        .Include(s => s.Cinema)
                        .Where(s => s.ShowtimeId == showtimeId)
                        .Select(s => s.CinemaId)
                        .FirstOrDefaultAsync();

                    if (showtimeCinema > 0)
                    {
                        var hasAccess = await _employeeCinemaAssignmentService.HasAccessToCinemaAsync(employee.EmployeeId, showtimeCinema);
                        if (!hasAccess)
                        {
                            throw new UnauthorizedException("Bạn không có quyền truy cập rạp này. Vui lòng liên hệ Partner để được phân quyền.");
                        }
                    }
                }
            }

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .Include(s => s.Screen)
                .Where(s => s.ShowtimeId == showtimeId && s.Cinema.PartnerId == partnerId)
                .Select(s => new PartnerShowtimeDetailResponse
                {
                    ShowtimeId = s.ShowtimeId,
                    MovieId = s.MovieId,
                    ScreenId = s.ScreenId,
                    CinemaId = s.CinemaId,
                    StartTime = s.ShowDatetime,
                    EndTime = s.EndTime ?? DateTime.MinValue,
                    BasePrice = s.BasePrice,
                    FormatType = s.FormatType,
                    AvailableSeats = s.AvailableSeats ?? 0,
                    Status = s.Status,
                    Movie = new ShowtimeMovieInfo
                    {
                        MovieId = s.Movie.MovieId,
                        Title = s.Movie.Title,
                        Description = s.Movie.Description ?? string.Empty,
                        PosterUrl = s.Movie.PosterUrl ?? string.Empty,
                        Duration = s.Movie.DurationMinutes,
                        Genre = s.Movie.Genre ?? string.Empty,
                        Language = s.Movie.Language ?? string.Empty
                    },
                    Cinema = new ShowtimeCinemaInfo
                    {
                        CinemaId = s.Cinema.CinemaId,
                        Name = s.Cinema.CinemaName,
                        Address = s.Cinema.Address ?? string.Empty,
                        City = s.Cinema.City ?? string.Empty,
                        District = s.Cinema.District ?? string.Empty,
                        Email = s.Cinema.Email ?? string.Empty
                    },
                    Screen = new ShowtimeScreenInfo
                    {
                        ScreenId = s.Screen.ScreenId,
                        Name = s.Screen.ScreenName,
                        ScreenType = s.Screen.ScreenType ?? string.Empty,
                        SoundSystem = s.Screen.SoundSystem ?? string.Empty,
                        Description = s.Screen.Description ?? string.Empty,
                        SeatRows = s.Screen.SeatRows ?? 0,
                        SeatColumns = s.Screen.SeatColumns ?? 0,
                        Capacity = s.Screen.Capacity ?? 0
                    }
                })
                .FirstOrDefaultAsync();

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu với ID đã cho");
            }

            return showtime;
        }

        public async Task<PartnerShowtimeDetailResponse> GetShowtimeByIdPublicAsync(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .Include(s => s.Screen)
                .Where(s => s.ShowtimeId == showtimeId && s.Status != "disabled")
                .Select(s => new PartnerShowtimeDetailResponse
                {
                    ShowtimeId = s.ShowtimeId,
                    MovieId = s.MovieId,
                    ScreenId = s.ScreenId,
                    CinemaId = s.CinemaId,
                    StartTime = s.ShowDatetime,
                    EndTime = s.EndTime ?? DateTime.MinValue,
                    BasePrice = s.BasePrice,
                    FormatType = s.FormatType,
                    AvailableSeats = s.AvailableSeats ?? 0,
                    Status = s.Status,
                    Movie = new ShowtimeMovieInfo
                    {
                        MovieId = s.Movie.MovieId,
                        Title = s.Movie.Title,
                        Description = s.Movie.Description ?? string.Empty,
                        PosterUrl = s.Movie.PosterUrl ?? string.Empty,
                        Duration = s.Movie.DurationMinutes,
                        Genre = s.Movie.Genre ?? string.Empty,
                        Language = s.Movie.Language ?? string.Empty
                    },
                    Cinema = new ShowtimeCinemaInfo
                    {
                        CinemaId = s.Cinema.CinemaId,
                        Name = s.Cinema.CinemaName,
                        Address = s.Cinema.Address ?? string.Empty,
                        City = s.Cinema.City ?? string.Empty,
                        District = s.Cinema.District ?? string.Empty,
                        Email = s.Cinema.Email ?? string.Empty
                    },
                    Screen = new ShowtimeScreenInfo
                    {
                        ScreenId = s.Screen.ScreenId,
                        Name = s.Screen.ScreenName,
                        ScreenType = s.Screen.ScreenType ?? string.Empty,
                        SoundSystem = s.Screen.SoundSystem ?? string.Empty,
                        Description = s.Screen.Description ?? string.Empty,
                        SeatRows = s.Screen.SeatRows ?? 0,
                        SeatColumns = s.Screen.SeatColumns ?? 0,
                        Capacity = s.Screen.Capacity ?? 0
                    }
                })
                .FirstOrDefaultAsync();

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu với ID đã cho");
            }

            return showtime;
        }

        public async Task<PartnerShowtimeListResponse> GetPartnerShowtimesAsync(int partnerId, int userId, PartnerShowtimeQueryRequest request)
        {
            // Validate parameters
            ValidateQueryParameters(request);

            // Base query - chỉ lấy showtimes của partner và status hợp lệ
            var baseQuery = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .Include(s => s.Screen)
                .Where(s => s.Cinema.PartnerId == partnerId &&
                           s.Status != "disabled" && // Loại bỏ các showtime đã bị disabled
                           (s.Status == "scheduled" || s.Status == "finished")); // Chỉ lấy scheduled và finished

            // Nếu là Staff, chỉ lấy showtimes của các rạp được phân quyền
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.UserType == "Staff" || user?.UserType == "Marketing" || user?.UserType == "Cashier")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee != null)
                {
                    // Lấy danh sách cinemaIds được phân quyền cho Staff
                    var assignedCinemaIds = await _context.EmployeeCinemaAssignments
                        .Where(eca => eca.EmployeeId == employee.EmployeeId && eca.IsActive)
                        .Select(eca => eca.CinemaId)
                        .ToListAsync();

                    if (assignedCinemaIds.Count == 0)
                    {
                        // Staff chưa được phân quyền rạp nào
                        return new PartnerShowtimeListResponse
                        {
                            Showtimes = new List<PartnerShowtimeListItem>(),
                            Total = 0,
                            Page = request.Page,
                            Limit = request.Limit,
                            TotalPages = 0
                        };
                    }

                    // Filter chỉ lấy showtimes của các rạp được phân quyền
                    baseQuery = baseQuery.Where(s => assignedCinemaIds.Contains(s.CinemaId));
                }
            }

            // Apply filters
            var filteredQuery = ApplyFilters(baseQuery, request);

            // Get total count for pagination
            var totalCount = await filteredQuery.CountAsync();

            // Apply sorting
            var sortedQuery = ApplySorting(filteredQuery, request);

            // Apply pagination
            var showtimes = await sortedQuery
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .Select(s => new PartnerShowtimeListItem
                {
                    ShowtimeId = s.ShowtimeId.ToString(),
                    MovieId = s.MovieId.ToString(),
                    ScreenId = s.ScreenId.ToString(),
                    CinemaId = s.CinemaId.ToString(),
                    StartTime = s.ShowDatetime,
                    EndTime = s.EndTime ?? DateTime.MinValue,
                    BasePrice = s.BasePrice.ToString("F2"),
                    FormatType = s.FormatType.ToLower(),
                    AvailableSeats = s.AvailableSeats ?? 0,
                    Status = s.Status,
                    Movie = new ShowtimeMovieInfo
                    {
                        MovieId = s.Movie.MovieId,
                        Title = s.Movie.Title,
                        Description = s.Movie.Description ?? string.Empty,
                        PosterUrl = s.Movie.PosterUrl ?? string.Empty,
                        Duration = s.Movie.DurationMinutes,
                        Genre = s.Movie.Genre ?? string.Empty,
                        Language = s.Movie.Language ?? string.Empty
                    },
                    Cinema = new ShowtimeCinemaInfo
                    {
                        CinemaId = s.Cinema.CinemaId,
                        Name = s.Cinema.CinemaName,
                        Address = s.Cinema.Address ?? string.Empty,
                        City = s.Cinema.City ?? string.Empty,
                        District = s.Cinema.District ?? string.Empty,
                        Email = s.Cinema.Email ?? string.Empty
                    },
                    Screen = new ShowtimeScreenInfo
                    {
                        ScreenId = s.Screen.ScreenId,
                        Name = s.Screen.ScreenName,
                        ScreenType = s.Screen.ScreenType == null ? string.Empty : s.Screen.ScreenType.ToLower(),
                        SoundSystem = s.Screen.SoundSystem ?? string.Empty,
                        Description = s.Screen.Description ?? string.Empty,
                        SeatRows = s.Screen.SeatRows ?? 0,
                        SeatColumns = s.Screen.SeatColumns ?? 0,
                        Capacity = s.Screen.Capacity ?? 0
                    }
                })
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.Limit);

            return new PartnerShowtimeListResponse
            {
                Showtimes = showtimes,
                Total = totalCount,
                Page = request.Page,
                Limit = request.Limit,
                TotalPages = totalPages
            };
        }

        private IQueryable<Showtime> ApplyFilters(IQueryable<Showtime> query, PartnerShowtimeQueryRequest request)
        {
            // Filter by movie_id
            if (!string.IsNullOrEmpty(request.MovieId) && int.TryParse(request.MovieId, out int movieId))
            {
                query = query.Where(s => s.MovieId == movieId);
            }

            // Filter by cinema_id
            if (!string.IsNullOrEmpty(request.CinemaId) && int.TryParse(request.CinemaId, out int cinemaId))
            {
                query = query.Where(s => s.CinemaId == cinemaId);
            }

            // Filter by screen_id
            if (!string.IsNullOrEmpty(request.ScreenId) && int.TryParse(request.ScreenId, out int screenId))
            {
                query = query.Where(s => s.ScreenId == screenId);
            }

            // Filter by date (YYYY-MM-DD)
            if (!string.IsNullOrEmpty(request.Date) && DateOnly.TryParse(request.Date, out DateOnly filterDate))
            {
                query = query.Where(s => DateOnly.FromDateTime(s.ShowDatetime) == filterDate);
            }

            // Filter by status - chỉ cho phép scheduled và finished
            if (!string.IsNullOrEmpty(request.Status))
            {
                var validStatuses = new[] { "scheduled", "finished" };
                var status = request.Status.ToLower();

                if (validStatuses.Contains(status))
                {
                    query = query.Where(s => s.Status == status);
                }
            }

            return query;
        }

        private IQueryable<Showtime> ApplySorting(IQueryable<Showtime> query, PartnerShowtimeQueryRequest request)
        {
            var isDescending = request.SortOrder?.ToLower() == "desc";

            return request.SortBy?.ToLower() switch
            {
                "start_time" => isDescending ?
                    query.OrderByDescending(s => s.ShowDatetime) :
                    query.OrderBy(s => s.ShowDatetime),
                "base_price" => isDescending ?
                    query.OrderByDescending(s => s.BasePrice) :
                    query.OrderBy(s => s.BasePrice),
                "available_seats" => isDescending ?
                    query.OrderByDescending(s => s.AvailableSeats) :
                    query.OrderBy(s => s.AvailableSeats),
                _ => isDescending ?
                    query.OrderByDescending(s => s.ShowDatetime) :
                    query.OrderBy(s => s.ShowDatetime)
            };
        }

        private void ValidateQueryParameters(PartnerShowtimeQueryRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (request.Page < 1)
            {
                errors["page"] = new ValidationError
                {
                    Msg = "Page phải lớn hơn hoặc bằng 1",
                    Path = "page"
                };
            }

            if (request.Limit < 1 || request.Limit > 100)
            {
                errors["limit"] = new ValidationError
                {
                    Msg = "Limit phải từ 1 đến 100",
                    Path = "limit"
                };
            }

            // Validate date format
            if (!string.IsNullOrEmpty(request.Date) && !DateOnly.TryParse(request.Date, out _))
            {
                errors["date"] = new ValidationError
                {
                    Msg = "Định dạng date không hợp lệ. Sử dụng YYYY-MM-DD",
                    Path = "date"
                };
            }

            // Validate status - chỉ cho phép scheduled và finished
            if (!string.IsNullOrEmpty(request.Status))
            {
                var validStatuses = new[] { "scheduled", "finished" };
                if (!validStatuses.Contains(request.Status.ToLower()))
                {
                    errors["status"] = new ValidationError
                    {
                        Msg = "Status không hợp lệ. Chỉ cho phép: scheduled, finished",
                        Path = "status"
                    };
                }
            }

            // Validate sort_order
            if (!string.IsNullOrEmpty(request.SortOrder) &&
                request.SortOrder.ToLower() != "asc" &&
                request.SortOrder.ToLower() != "desc")
            {
                errors["sort_order"] = new ValidationError
                {
                    Msg = "Sort order không hợp lệ. Chỉ cho phép: asc, desc",
                    Path = "sort_order"
                };
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private async Task ValidateMovieCreatableWindowAsync(int movieId)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId && m.IsActive);
            if (movie == null)
                throw new ValidationException("movie_id", "Phim không tồn tại hoặc không hoạt động");

            var today = TodayVN();

            if (today > movie.EndDate)
                throw new ValidationException("movie_id",
                    $"Phim đã kết thúc thời gian công chiếu. Ngày kết thúc: {movie.EndDate:dd/MM/yyyy}");

            var earliestCreatable = movie.PremiereDate.AddDays(-PRESALE_DAYS);
            if (today < earliestCreatable)
                throw new ValidationException("movie_id",
                    $"Chưa tới cửa sổ mở lịch. Có thể mở lịch từ {earliestCreatable:dd/MM/yyyy} (T-{PRESALE_DAYS} ngày).");
        }
        private async Task ValidateShowtimeWithinMovieWindowAsync(int movieId, DateTime startTime, DateTime endTime)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId);
            if (movie == null) return;

            var startDate = DateOnly.FromDateTime(startTime.AddHours(7)); // hoặc giữ nguyên nếu giờ vào đã là local
            var endDate = DateOnly.FromDateTime(endTime.AddHours(7));

            if (startDate < movie.PremiereDate)
                throw new ValidationException("start_time",
                    $"Thời gian bắt đầu phải không sớm hơn ngày công chiếu {movie.PremiereDate:dd/MM/yyyy}");

            if (endDate > movie.EndDate)
                throw new ValidationException("end_time",
                    $"Thời gian kết thúc phải không trễ hơn ngày kết thúc {movie.EndDate:dd/MM/yyyy}");
        }

        // Đổi sang dùng VN time cho thông báo “không thể quá khứ”
        private void ValidateShowtimeDateTime(DateTime startTime, DateTime endTime)
        {
            var now = NowVN();
            var errors = new Dictionary<string, ValidationError>();

            if (startTime <= now)
                errors["start_time"] = new ValidationError { Msg = "Thời gian bắt đầu không thể trong quá khứ", Path = "start_time" };

            if (endTime <= startTime)
                errors["end_time"] = new ValidationError { Msg = "Thời gian kết thúc phải sau thời gian bắt đầu", Path = "end_time" };

            if (errors.Any()) throw new ValidationException(errors);
        }
    }
}