using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ScreenService : IScreenService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmployeeCinemaAssignmentService _employeeCinemaAssignmentService;

        public ScreenService(CinemaDbCoreContext context, IAuditLogService auditLogService, IEmployeeCinemaAssignmentService employeeCinemaAssignmentService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _employeeCinemaAssignmentService = employeeCinemaAssignmentService;
        }

        public async Task<ScreenResponse> CreateScreenAsync(int cinemaId, CreateScreenRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateCreateScreenRequest(request);
            await ValidateCinemaAccessAsync(cinemaId, partnerId, userId);
            await ValidateScreenCodeUniqueAsync(request.Code, cinemaId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var screen = new Screen
            {
                CinemaId = cinemaId,
                ScreenName = request.ScreenName.Trim(),
                Code = request.Code.Trim().ToUpper(),
                Description = request.Description?.Trim(),
                ScreenType = request.ScreenType.Trim().ToLower(),
                SoundSystem = request.SoundSystem?.Trim(),
                Capacity = request.Capacity,
                SeatRows = request.SeatRows,
                SeatColumns = request.SeatColumns,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Screens.Add(screen);
            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_CREATE_SCREEN",
                tableName: "Screen",
                recordId: screen.ScreenId,
                beforeData: null,
                afterData: BuildScreenSnapshot(screen),
                metadata: new { cinemaId, partnerId, userId });

            return await MapToScreenResponseAsync(screen);
        }

        public async Task<ScreenResponse> GetScreenByIdAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            return await MapToScreenResponseAsync(screen);
        }

        public async Task<ScreenResponse> GetScreenByIdPublicAsync(int screenId)
        {
            // ==================== VALIDATION SECTION ====================
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            if (screen == null)
            {
                throw new NotFoundException("Không tìm thấy phòng với ID này");
            }

            if (screen.IsActive != true)
            {
                throw new NotFoundException("Phòng này đã bị vô hiệu hóa");
            }

            // ==================== BUSINESS LOGIC SECTION ====================
            return await MapToScreenResponseAsync(screen);
        }

        public async Task<PaginatedScreensResponse> GetScreensAsync(int cinemaId, int partnerId, int userId, int page = 1, int limit = 10,
            string? screenType = null, bool? isActive = null, string? sortBy = "screen_name", string? sortOrder = "asc")
        {
            // ==================== VALIDATION SECTION ====================
            ValidateGetScreensRequest(page, limit);
            await ValidateCinemaAccessAsync(cinemaId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var query = _context.Screens
                .Where(s => s.CinemaId == cinemaId)
                .Include(s => s.Cinema)
                .AsQueryable();

            // Apply filters
            query = ApplyScreenFilters(query, screenType, isActive);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplyScreenSorting(query, sortBy, sortOrder);

            // Apply pagination
            var screens = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var screenResponses = new List<ScreenResponse>();
            foreach (var screen in screens)
            {
                screenResponses.Add(await MapToScreenResponseAsync(screen));
            }

            var pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedScreensResponse
            {
                Screens = screenResponses,
                Pagination = pagination
            };
        }

        public async Task<ScreenResponse> UpdateScreenAsync(int screenId, UpdateScreenRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateUpdateScreenRequest(request);
            await ValidateScreenAccessAsync(screenId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            // Validate không thể deactivate nếu đã có seat layout
            if (!request.IsActive && screen.IsActive)
            {
                var hasSeatLayout = await _context.SeatMaps.AnyAsync(sm => sm.ScreenId == screenId);
                if (hasSeatLayout)
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["isActive"] = new ValidationError
                        {
                            Msg = "Không thể vô hiệu hóa phòng đã có layout ghế",
                            Path = "isActive"
                        }
                    });
                }
            }

            var beforeSnapshot = BuildScreenSnapshot(screen);

            screen.ScreenName = request.ScreenName.Trim();
            screen.Description = request.Description?.Trim();
            screen.ScreenType = request.ScreenType.Trim().ToLower();
            screen.SoundSystem = request.SoundSystem?.Trim();
            screen.Capacity = request.Capacity;
            screen.IsActive = request.IsActive;
            screen.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_UPDATE_SCREEN",
                tableName: "Screen",
                recordId: screen.ScreenId,
                beforeData: beforeSnapshot,
                afterData: BuildScreenSnapshot(screen),
                metadata: new { screen.CinemaId, partnerId, userId });

            return await MapToScreenResponseAsync(screen);
        }

        public async Task<ScreenActionResponse> DeleteScreenAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            // Validate không thể xóa nếu đã có seat layout
            var hasSeatLayout = await _context.SeatMaps.AnyAsync(sm => sm.ScreenId == screenId);
            if (hasSeatLayout)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["delete"] = new ValidationError
                    {
                        Msg = "Không thể xóa phòng đã có layout ghế",
                        Path = "screenId"
                    }
                });
            }

            var beforeSnapshot = BuildScreenSnapshot(screen);

            // SOFT DELETE - set IsActive = false
            screen.IsActive = false;
            screen.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_DELETE_SCREEN",
                tableName: "Screen",
                recordId: screen.ScreenId,
                beforeData: beforeSnapshot,
                afterData: BuildScreenSnapshot(screen),
                metadata: new { screen.CinemaId, partnerId, userId });

            return new ScreenActionResponse
            {
                ScreenId = screen.ScreenId,
                ScreenName = screen.ScreenName,
                Message = "Xóa phòng thành công",
                IsActive = screen.IsActive,
                UpdatedDate = screen.UpdatedDate ?? DateTime.UtcNow
            };
        }

        // ==================== PRIVATE METHODS ====================
        private static object BuildScreenSnapshot(Screen screen)
        {
            return new
            {
                screen.ScreenId,
                screen.CinemaId,
                screen.ScreenName,
                screen.Code,
                screen.Description,
                screen.ScreenType,
                screen.SoundSystem,
                screen.Capacity,
                screen.SeatRows,
                screen.SeatColumns,
                screen.IsActive,
                screen.CreatedDate,
                screen.UpdatedDate
            };
        }

        private async Task<ScreenResponse> MapToScreenResponseAsync(Screen screen)
        {
            var hasSeatLayout = await _context.SeatMaps.AnyAsync(sm => sm.ScreenId == screen.ScreenId);

            return new ScreenResponse
            {
                ScreenId = screen.ScreenId,
                CinemaId = screen.CinemaId,
                CinemaName = screen.Cinema?.CinemaName ?? string.Empty,
                ScreenName = screen.ScreenName,
                Code = screen.Code,
                Description = screen.Description,
                ScreenType = screen.ScreenType,
                SoundSystem = screen.SoundSystem,
                Capacity = screen.Capacity ?? 0 ,
                SeatRows = screen.SeatRows ?? 0 ,
                SeatColumns = screen.SeatColumns ?? 0,
                IsActive = screen.IsActive,
                HasSeatLayout = hasSeatLayout,
                CreatedDate = screen.CreatedDate ?? DateTime.UtcNow,
                UpdatedDate = screen.UpdatedDate ?? DateTime.UtcNow
            };
        }

        private async Task ValidateCinemaAccessAsync(int cinemaId, int partnerId, int userId)
        {
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            if (cinema.IsActive != true)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["cinema"] = new ValidationError { Msg = "Rạp đã bị vô hiệu hóa", Path = "cinemaId" }
                });
            }

            // Nếu là Staff, kiểm tra có được phân quyền rạp này không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.UserType == "Staff" || user?.UserType == "Marketing" || user?.UserType == "Cashier")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee != null)
                {
                    var hasAccess = await _employeeCinemaAssignmentService.HasAccessToCinemaAsync(employee.EmployeeId, cinemaId);
                    if (!hasAccess)
                    {
                        throw new UnauthorizedException("Bạn không có quyền truy cập rạp này. Vui lòng liên hệ Partner để được phân quyền.");
                    }
                }
            }
        }

        private async Task ValidateScreenAccessAsync(int screenId, int partnerId, int userId)
        {
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId && s.Cinema.PartnerId == partnerId);

            if (screen == null)
            {
                throw new NotFoundException("Không tìm thấy phòng với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            // Nếu là Staff, kiểm tra có được phân quyền rạp của phòng này không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.UserType == "Staff" || user?.UserType == "Marketing" || user?.UserType == "Cashier")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee != null)
                {
                    var hasAccess = await _employeeCinemaAssignmentService.HasAccessToCinemaAsync(employee.EmployeeId, screen.CinemaId);
                    if (!hasAccess)
                    {
                        throw new UnauthorizedException("Bạn không có quyền truy cập rạp này. Vui lòng liên hệ Partner để được phân quyền.");
                    }
                }
            }
        }

        private async Task ValidateScreenCodeUniqueAsync(string code, int cinemaId)
        {
            var existingScreen = await _context.Screens
                .FirstOrDefaultAsync(s => s.CinemaId == cinemaId && s.Code.ToUpper() == code.Trim().ToUpper());

            if (existingScreen != null)
            {
                throw new ConflictException("code", "Mã phòng đã tồn tại trong rạp này");
            }
        }

        private void ValidateCreateScreenRequest(CreateScreenRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.ScreenName))
                errors["screenName"] = new ValidationError { Msg = "Tên phòng là bắt buộc", Path = "screenName" };
            else if (request.ScreenName.Trim().Length > 255)
                errors["screenName"] = new ValidationError { Msg = "Tên phòng không được vượt quá 255 ký tự", Path = "screenName" };

            if (string.IsNullOrWhiteSpace(request.Code))
                errors["code"] = new ValidationError { Msg = "Mã phòng là bắt buộc", Path = "code" };
            else if (request.Code.Trim().Length > 50)
                errors["code"] = new ValidationError { Msg = "Mã phòng không được vượt quá 50 ký tự", Path = "code" };
            else if (!Regex.IsMatch(request.Code, @"^[A-Z0-9_]+$", RegexOptions.IgnoreCase))
                errors["code"] = new ValidationError { Msg = "Mã phòng chỉ được chứa chữ cái, số và dấu gạch dưới", Path = "code" };

            var validScreenTypes = new[] { "standard", "premium", "imax", "4dx" };
            if (!validScreenTypes.Contains(request.ScreenType.ToLower()))
                errors["screenType"] = new ValidationError { Msg = $"Loại phòng phải là: {string.Join(", ", validScreenTypes)}", Path = "screenType" };

            if (request.Capacity <= 0)
                errors["capacity"] = new ValidationError { Msg = "Sức chứa phải lớn hơn 0", Path = "capacity" };

            if (request.SeatRows <= 0)
                errors["seatRows"] = new ValidationError { Msg = "Số hàng ghế phải lớn hơn 0", Path = "seatRows" };

            if (request.SeatColumns <= 0)
                errors["seatColumns"] = new ValidationError { Msg = "Số cột ghế phải lớn hơn 0", Path = "seatColumns" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateUpdateScreenRequest(UpdateScreenRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.ScreenName))
                errors["screenName"] = new ValidationError { Msg = "Tên phòng là bắt buộc", Path = "screenName" };
            else if (request.ScreenName.Trim().Length > 255)
                errors["screenName"] = new ValidationError { Msg = "Tên phòng không được vượt quá 255 ký tự", Path = "screenName" };

            var validScreenTypes = new[] { "standard", "premium", "imax", "4dx" };
            if (!validScreenTypes.Contains(request.ScreenType.ToLower()))
                errors["screenType"] = new ValidationError { Msg = $"Loại phòng phải là: {string.Join(", ", validScreenTypes)}", Path = "screenType" };

            if (request.Capacity <= 0)
                errors["capacity"] = new ValidationError { Msg = "Sức chứa phải lớn hơn 0", Path = "capacity" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateGetScreensRequest(int page, int limit)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (page < 1)
                errors["page"] = new ValidationError { Msg = "Số trang phải lớn hơn 0", Path = "page" };

            if (limit < 1 || limit > 100)
                errors["limit"] = new ValidationError { Msg = "Số lượng mỗi trang phải từ 1 đến 100", Path = "limit" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private IQueryable<Screen> ApplyScreenFilters(IQueryable<Screen> query, string? screenType, bool? isActive)
        {
            if (!string.IsNullOrWhiteSpace(screenType))
            {
                query = query.Where(s => s.ScreenType.ToLower() == screenType.Trim().ToLower());
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            return query;
        }

        private IQueryable<Screen> ApplyScreenSorting(IQueryable<Screen> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "screen_name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "code" => isAscending ? query.OrderBy(s => s.Code) : query.OrderByDescending(s => s.Code),
                "screen_type" => isAscending ? query.OrderBy(s => s.ScreenType) : query.OrderByDescending(s => s.ScreenType),
                "capacity" => isAscending ? query.OrderBy(s => s.Capacity) : query.OrderByDescending(s => s.Capacity),
                "created_date" => isAscending ? query.OrderBy(s => s.CreatedDate) : query.OrderByDescending(s => s.CreatedDate),
                "updated_date" => isAscending ? query.OrderBy(s => s.UpdatedDate) : query.OrderByDescending(s => s.UpdatedDate),
                _ => isAscending ? query.OrderBy(s => s.ScreenName) : query.OrderByDescending(s => s.ScreenName)
            };
        }
    }
}