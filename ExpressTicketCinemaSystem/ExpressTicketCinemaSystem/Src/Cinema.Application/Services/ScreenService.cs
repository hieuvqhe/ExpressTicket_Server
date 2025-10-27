using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ScreenService
    {
        private readonly CinemaDbCoreContext _context;

        public ScreenService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<CreateScreenResponse> CreateScreenAsync(int cinemaId, int partnerId, CreateScreenRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateCreateScreenRequest(request);
            await ValidateCinemaOwnershipAsync(cinemaId, partnerId);
            await ValidateScreenNameAsync(cinemaId, request.Name);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Tạo screen mới
            var screen = new Screen
            {
                CinemaId = cinemaId,
                ScreenName = request.Name,
                ScreenType = request.ScreenType,
                IsActive = request.Status.ToLower() == "active",
                Capacity = request.Capacity,
                CreatedAt = DateTime.UtcNow // Tự động lưu thời gian tạo
            };

            _context.Screens.Add(screen);
            await _context.SaveChangesAsync();

            // Tạo các seat từ seat_layout
            await CreateSeatsFromLayoutAsync(screen.ScreenId, request.SeatLayout);

            return new CreateScreenResponse
            {
                ScreenId = screen.ScreenId
            };
        }

        private void ValidateCreateScreenRequest(CreateScreenRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError { Msg = "Tên screen là bắt buộc", Path = "name" };

            if (request.Capacity <= 0)
                errors["capacity"] = new ValidationError { Msg = "Capacity phải lớn hơn 0", Path = "capacity" };

            if (string.IsNullOrWhiteSpace(request.ScreenType))
                errors["screen_type"] = new ValidationError { Msg = "Loại screen là bắt buộc", Path = "screen_type" };

            // Validate screen_type với các giá trị cho phép
            var validScreenTypes = new[] { "standard", "premium", "imax", "3d", "4dx" };
            if (!string.IsNullOrWhiteSpace(request.ScreenType) &&
                !validScreenTypes.Contains(request.ScreenType.ToLower()))
            {
                errors["screen_type_invalid"] = new ValidationError
                {
                    Msg = "Loại screen không hợp lệ. Chỉ chấp nhận: standard, premium, imax, 3d, 4dx",
                    Path = "screen_type"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Status))
                errors["status"] = new ValidationError { Msg = "Status là bắt buộc", Path = "status" };

            var validStatuses = new[] { "active", "inactive", "maintenance" };
            if (!validStatuses.Contains(request.Status.ToLower()))
                errors["status_invalid"] = new ValidationError { Msg = "Status không hợp lệ", Path = "status" };

            // Validate seat layout
            if (request.SeatLayout == null || !request.SeatLayout.Any())
                errors["seat_layout"] = new ValidationError { Msg = "Seat layout là bắt buộc", Path = "seat_layout" };
            else
                ValidateSeatLayout(request.SeatLayout, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateScreenNameAsync(int cinemaId, string screenName)
        {
            // Kiểm tra xem trong cinema này đã có screen với tên này chưa
            var existingScreen = await _context.Screens
                .FirstOrDefaultAsync(s => s.CinemaId == cinemaId && s.ScreenName == screenName);

            if (existingScreen != null)
            {
                throw new ConflictException("name", $"Tên screen '{screenName}' đã tồn tại trong cinema này");
            }
        }

        private void ValidateSeatLayout(List<List<SeatLayoutRequest>> seatLayout, Dictionary<string, ValidationError> errors)
        {
            var seatNumbers = new HashSet<string>();

            for (int i = 0; i < seatLayout.Count; i++)
            {
                var row = seatLayout[i];
                if (row == null || !row.Any())
                {
                    errors.TryAdd($"row_{i}_empty", new ValidationError { Msg = $"Hàng {i} không có ghế", Path = "seat_layout" });
                    continue;
                }

                foreach (var seat in row)
                {
                    if (seat == null)
                    {
                        errors.TryAdd($"seat_null_{i}", new ValidationError { Msg = $"Có ghế null trong hàng {i}", Path = "seat_layout" });
                        continue;
                    }

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(seat.Row))
                        errors.TryAdd($"seat_row_{i}", new ValidationError { Msg = "Row là bắt buộc", Path = "seat_layout" });

                    if (seat.Number <= 0)
                        errors.TryAdd($"seat_number_{i}", new ValidationError { Msg = "Số ghế phải lớn hơn 0", Path = "seat_layout" });

                    if (string.IsNullOrWhiteSpace(seat.Type))
                        errors.TryAdd($"seat_type_{i}", new ValidationError { Msg = "Loại ghế là bắt buộc", Path = "seat_layout" });

                    if (string.IsNullOrWhiteSpace(seat.Status))
                        errors.TryAdd($"seat_status_{i}", new ValidationError { Msg = "Status ghế là bắt buộc", Path = "seat_layout" });

                    // Check duplicate seats
                    var seatKey = $"{seat.Row}-{seat.Number}";
                    if (seatNumbers.Contains(seatKey))
                        errors.TryAdd($"seat_duplicate_{seatKey}", new ValidationError { Msg = $"Ghế {seatKey} bị trùng", Path = "seat_layout" });
                    else
                        seatNumbers.Add(seatKey);
                }
            }
        }

        private async Task ValidateCinemaOwnershipAsync(int cinemaId, int partnerId)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Partner)
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId);

            if (cinema == null)
                throw new NotFoundException("Không tìm thấy cinema với ID này.");

            // Kiểm tra cinema thuộc về partner đang đăng nhập
            if (cinema.Partner?.UserId != partnerId)
                throw new UnauthorizedException("Bạn không có quyền truy cập cinema này.");

            // Kiểm tra partner có được approved không
            if (cinema.Partner.Status != "approved")
                throw new UnauthorizedException("Tài khoản partner chưa được approved.");
        }

        
        private async Task CreateSeatsFromLayoutAsync(int screenId, List<List<SeatLayoutRequest>> seatLayout)
        {
            var seats = new List<Seat>();

            foreach (var row in seatLayout)
            {
                foreach (var seatRequest in row)
                {
                    var seat = new Seat
                    {
                        ScreenId = screenId,
                        RowCode = seatRequest.Row,
                        SeatNumber = seatRequest.Number,
                      //  SeatType = seatRequest.Type,
                        Status = seatRequest.Status
                    };

                    seats.Add(seat);
                }
            }

            if (seats.Any())
            {
                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UpdateScreenResponse> UpdateScreenAsync(int screenId, int partnerId, UpdateScreenRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateUpdateScreenRequest(request);

            // Lấy screen và kiểm tra quyền sở hữu
            var screen = await GetScreenWithOwnershipAsync(screenId, partnerId);

            // Validate screen name (trừ screen hiện tại)
            await ValidateScreenNameForUpdateAsync(screen.CinemaId, request.Name, screenId);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật thông tin screen
            screen.ScreenName = request.Name;
            screen.ScreenType = request.ScreenType;
            screen.IsActive = request.Status.ToLower() == "active";
            screen.Capacity = request.Capacity;
            screen.UpdatedAt = DateTime.UtcNow;

            // Xóa các seat cũ và tạo seat mới từ layout
            await UpdateSeatsFromLayoutAsync(screenId, request.SeatLayout);

            await _context.SaveChangesAsync();

            return new UpdateScreenResponse
            {
                ScreenId = screen.ScreenId
            };
        }

        private void ValidateUpdateScreenRequest(UpdateScreenRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError { Msg = "Tên screen là bắt buộc", Path = "name" };

            if (request.Capacity <= 0)
                errors["capacity"] = new ValidationError { Msg = "Capacity phải lớn hơn 0", Path = "capacity" };

            if (string.IsNullOrWhiteSpace(request.ScreenType))
                errors["screen_type"] = new ValidationError { Msg = "Loại screen là bắt buộc", Path = "screen_type" };

            // Validate screen_type với các giá trị cho phép
            var validScreenTypes = new[] { "standard", "premium", "imax", "3d", "4dx" };
            if (!string.IsNullOrWhiteSpace(request.ScreenType) &&
                !validScreenTypes.Contains(request.ScreenType.ToLower()))
            {
                errors["screen_type_invalid"] = new ValidationError
                {
                    Msg = "Loại screen không hợp lệ. Chỉ chấp nhận: standard, premium, imax, 3d, 4dx",
                    Path = "screen_type"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Status))
                errors["status"] = new ValidationError { Msg = "Status là bắt buộc", Path = "status" };

            var validStatuses = new[] { "active", "inactive", "maintenance" };
            if (!validStatuses.Contains(request.Status.ToLower()))
                errors["status_invalid"] = new ValidationError { Msg = "Status không hợp lệ", Path = "status" };

            // Validate seat layout
            if (request.SeatLayout == null || !request.SeatLayout.Any())
                errors["seat_layout"] = new ValidationError { Msg = "Seat layout là bắt buộc", Path = "seat_layout" };
            else
                ValidateSeatLayout(request.SeatLayout, errors);

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task<Screen> GetScreenWithOwnershipAsync(int screenId, int partnerId)
        {
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                    .ThenInclude(c => c.Partner)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            if (screen == null)
                throw new NotFoundException("Không tìm thấy screen với ID này.");

            // Kiểm tra cinema thuộc về partner đang đăng nhập
            if (screen.Cinema.Partner?.UserId != partnerId)
                throw new UnauthorizedException("Bạn không có quyền truy cập screen này.");

            // Kiểm tra partner có được approved không
            if (screen.Cinema.Partner.Status != "approved")
                throw new UnauthorizedException("Tài khoản partner chưa được approved.");

            return screen;
        }

        private async Task ValidateScreenNameForUpdateAsync(int cinemaId, string screenName, int currentScreenId)
        {
            // Kiểm tra xem trong cinema này đã có screen với tên này chưa (trừ screen hiện tại)
            var existingScreen = await _context.Screens
                .FirstOrDefaultAsync(s => s.CinemaId == cinemaId &&
                                         s.ScreenName == screenName &&
                                         s.ScreenId != currentScreenId);

            if (existingScreen != null)
            {
                throw new ConflictException("name", $"Tên screen '{screenName}' đã tồn tại trong cinema này");
            }
        }

        private async Task UpdateSeatsFromLayoutAsync(int screenId, List<List<SeatLayoutRequest>> seatLayout)
        {
            // Xóa tất cả seat cũ của screen
            var existingSeats = await _context.Seats
                .Where(s => s.ScreenId == screenId)
                .ToListAsync();

            if (existingSeats.Any())
            {
                _context.Seats.RemoveRange(existingSeats);
                await _context.SaveChangesAsync();
            }

            // Tạo seat mới từ layout
            var seats = new List<Seat>();

            foreach (var row in seatLayout)
            {
                foreach (var seatRequest in row)
                {
                    var seat = new Seat
                    {
                        ScreenId = screenId,
                        RowCode = seatRequest.Row,
                        SeatNumber = seatRequest.Number,
                      //  SeatType = seatRequest.Type,
                        Status = seatRequest.Status
                    };

                    seats.Add(seat);
                }
            }

            if (seats.Any())
            {
                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<GetScreenResponse> GetScreenByIdAsync(int screenId, int partnerId)
        {
            // Lấy screen và kiểm tra quyền sở hữu
            var screen = await GetScreenWithOwnershipAsync(screenId, partnerId);

            // Lấy seat layout từ database
            var seats = await _context.Seats
                .Where(s => s.ScreenId == screenId)
                .OrderBy(s => s.RowCode)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

            // Chuyển đổi seats thành seat layout structure
            var seatLayout = ConvertSeatsToLayout(seats);

            return new GetScreenResponse
            {
                ScreenId = screen.ScreenId,
                CinemaId = screen.CinemaId,
                Name = screen.ScreenName,
                SeatLayout = seatLayout,
                Capacity = screen.Capacity,
                ScreenType = screen.ScreenType ?? "standard",
                Status = screen.IsActive ? "active" : "inactive",
                CreatedAt = screen.CreatedAt,
                UpdatedAt = screen.UpdatedAt
            };
        }

        private List<List<SeatLayoutResponse>> ConvertSeatsToLayout(List<Seat> seats)
        {
            var seatLayout = new List<List<SeatLayoutResponse>>();

            if (!seats.Any())
                return seatLayout;

            // Nhóm seats theo row
            var seatsByRow = seats
                .GroupBy(s => s.RowCode)
                .OrderBy(g => g.Key);

            foreach (var rowGroup in seatsByRow)
            {
                var rowSeats = rowGroup
                    .OrderBy(s => s.SeatNumber)
                    .Select(seat => new SeatLayoutResponse
                    {
                      //  Row = seat.RowCode,
                      //  Number = seat.SeatNumber,
                     //   Type = seat.SeatType ?? "regular",
                      //  Status = seat.Status ?? "active"
                    })
                    .ToList();

                seatLayout.Add(rowSeats);
            }

            return seatLayout;
        }

        public async Task<GetAllScreenResponse> GetScreensAsync(
    int cinemaId,
    int partnerId,
    int page = 1,
    int limit = 10,
    string? screenType = null,
    string? status = null,
    string? sortBy = "name",
    string? sortOrder = "asc")
        {
            // Kiểm tra quyền sở hữu cinema
            await ValidateCinemaOwnershipAsync(cinemaId, partnerId);

            // Validate pagination
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            // Base query
            var query = _context.Screens
                .Where(s => s.CinemaId == cinemaId)
                .AsQueryable();

            // Apply filters
            query = ApplyFilters(query, screenType, status);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, sortBy, sortOrder);

            // Apply pagination
            var screens = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // Lấy seat layout cho từng screen
            var screenItems = new List<ScreenItemResponse>();
            foreach (var screen in screens)
            {
                var seats = await _context.Seats
                    .Where(s => s.ScreenId == screen.ScreenId)
                    .OrderBy(s => s.RowCode)
                    .ThenBy(s => s.SeatNumber)
                    .ToListAsync();

                var seatLayout = ConvertSeatsToLayoutForGetAll(seats);

                screenItems.Add(new ScreenItemResponse
                {
                    ScreenId = screen.ScreenId,
                    CinemaId = screen.CinemaId,
                    Name = screen.ScreenName,
                    SeatLayout = seatLayout,
                    Capacity = screen.Capacity,
                    ScreenType = screen.ScreenType ?? "standard",
                    Status = screen.IsActive ? "active" : "inactive",
                    CreatedAt = screen.CreatedAt,
                    UpdatedAt = screen.UpdatedAt
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)limit);

            return new GetAllScreenResponse
            {
                Screens = screenItems,
                Total = totalCount,
                Page = page,
                Limit = limit,
                TotalPages = totalPages
            };
        }

        private List<List<GetAllSeatLayoutResponse>> ConvertSeatsToLayoutForGetAll(List<Seat> seats)
        {
            var seatLayout = new List<List<GetAllSeatLayoutResponse>>();

            if (!seats.Any())
                return seatLayout;

            // Nhóm seats theo row
            var seatsByRow = seats
                .GroupBy(s => s.RowCode)
                .OrderBy(g => g.Key);

            foreach (var rowGroup in seatsByRow)
            {
                var rowSeats = rowGroup
                    .OrderBy(s => s.SeatNumber)
                    .Select(seat => new GetAllSeatLayoutResponse
                    {
                        Row = seat.RowCode,
                        Number = seat.SeatNumber,
                      //  Type = seat.SeatType ?? "regular",
                        Status = seat.Status ?? "active"
                    })
                    .ToList();

                seatLayout.Add(rowSeats);
            }

            return seatLayout;
        }

        private IQueryable<Screen> ApplyFilters(IQueryable<Screen> query, string? screenType, string? status)
        {
            // Filter by screen_type
            if (!string.IsNullOrWhiteSpace(screenType))
            {
                var validScreenTypes = new[] { "standard", "premium", "imax", "3d", "4dx" };
                if (validScreenTypes.Contains(screenType.ToLower()))
                {
                    query = query.Where(s => s.ScreenType.ToLower() == screenType.ToLower());
                }
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(s => s.IsActive);
                }
                else if (status.ToLower() == "inactive")
                {
                    query = query.Where(s => !s.IsActive);
                }
            }

            return query;
        }

        private IQueryable<Screen> ApplySorting(IQueryable<Screen> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "screen_id" => isAscending ? query.OrderBy(s => s.ScreenId) : query.OrderByDescending(s => s.ScreenId),
                "name" => isAscending ? query.OrderBy(s => s.ScreenName) : query.OrderByDescending(s => s.ScreenName),
                "capacity" => isAscending ? query.OrderBy(s => s.Capacity) : query.OrderByDescending(s => s.Capacity),
                "screen_type" => isAscending ? query.OrderBy(s => s.ScreenType) : query.OrderByDescending(s => s.ScreenType),
                "status" => isAscending ? query.OrderBy(s => s.IsActive) : query.OrderByDescending(s => s.IsActive),
                "created_at" => isAscending ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt),
                "updated_at" => isAscending ? query.OrderBy(s => s.UpdatedAt) : query.OrderByDescending(s => s.UpdatedAt),
                _ => isAscending ? query.OrderBy(s => s.ScreenName) : query.OrderByDescending(s => s.ScreenName) // default
            };
        }

        public async Task<DeleteScreenResponse> DeleteScreenAsync(int screenId, int partnerId)
        {
            // ==================== VALIDATION SECTION ====================

            // Lấy screen và kiểm tra quyền sở hữu
            var screen = await GetScreenWithOwnershipAsync(screenId, partnerId);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Xóa tất cả seat liên quan đến screen trước (đảm bảo foreign key constraint)
            await DeleteSeatsByScreenIdAsync(screenId);

            // Xóa screen
            _context.Screens.Remove(screen);
            await _context.SaveChangesAsync();

            return new DeleteScreenResponse
            {
                ScreenId = screenId,
                Message = "xóa thành công"
            };
        }

        private async Task DeleteSeatsByScreenIdAsync(int screenId)
        {
            var seats = await _context.Seats
                .Where(s => s.ScreenId == screenId)
                .ToListAsync();

            if (seats.Any())
            {
                _context.Seats.RemoveRange(seats);
                await _context.SaveChangesAsync();
            }
        }


    }
}