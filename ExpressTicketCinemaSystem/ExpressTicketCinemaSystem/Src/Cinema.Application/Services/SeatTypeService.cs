using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Text.RegularExpressions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using Microsoft.Data.SqlClient;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface ISeatTypeService
    {
        Task<PaginatedSeatTypesResponse> GetSeatTypesAsync(GetSeatTypesRequest request, int partnerId, int userId);
        Task<SeatTypeDetailResponse> GetSeatTypeByIdAsync(int seatTypeId, int partnerId, int userId);
        Task<SeatTypeActionResponse> CreateSeatTypeAsync(CreateSeatTypeRequest request, int partnerId, int userId);
        Task<SeatTypeActionResponse> UpdateSeatTypeAsync(int seatTypeId, UpdateSeatTypeRequest request, int partnerId, int userId);
        Task<SeatTypeActionResponse> DeleteSeatTypeAsync(int seatTypeId, int partnerId, int userId);
    }

    public class SeatTypeService : ISeatTypeService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;

        public SeatTypeService(CinemaDbCoreContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<PaginatedSeatTypesResponse> GetSeatTypesAsync(GetSeatTypesRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateGetSeatTypesRequest(request);
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Base query - CHỈ lấy seat types của partner hiện tại
            var query = _context.SeatTypes
                .Where(st => st.PartnerId == partnerId)
                .AsQueryable();

            // Apply filters
            query = ApplySeatTypeFilters(query, request);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySeatTypeSorting(query, request.SortBy, request.SortOrder);

            // Apply pagination
            var seatTypes = await query
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .Select(st => new SeatTypeResponse
                {
                    Id = st.Id,
                    Code = st.Code,
                    Name = st.Name,
                    Surcharge = st.Surcharge,
                    Color = st.Color,
                    Description = st.Description,
                    Status = st.Status,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt
                })
                .ToListAsync();

            // Pagination metadata
            var pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
            {
                CurrentPage = request.Page,
                PageSize = request.Limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.Limit)
            };

            return new PaginatedSeatTypesResponse
            {
                SeatTypes = seatTypes,
                Pagination = pagination
            };
        }

        private void ValidateGetSeatTypesRequest(GetSeatTypesRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validate pagination
            if (request.Page < 1)
                errors["page"] = new ValidationError { Msg = "Số trang phải lớn hơn 0", Path = "page" };

            if (request.Limit < 1 || request.Limit > 100)
                errors["limit"] = new ValidationError { Msg = "Số lượng mỗi trang phải từ 1 đến 100", Path = "limit" };

            // Validate surcharge range
            if (request.MinSurcharge.HasValue && request.MinSurcharge < 0)
                errors["minSurcharge"] = new ValidationError { Msg = "Phụ thu tối thiểu không thể âm", Path = "minSurcharge" };

            if (request.MaxSurcharge.HasValue && request.MaxSurcharge < 0)
                errors["maxSurcharge"] = new ValidationError { Msg = "Phụ thu tối đa không thể âm", Path = "maxSurcharge" };

            if (request.MinSurcharge.HasValue && request.MaxSurcharge.HasValue && request.MinSurcharge > request.MaxSurcharge)
                errors["surchargeRange"] = new ValidationError { Msg = "Phụ thu tối thiểu không thể lớn hơn phụ thu tối đa", Path = "minSurcharge" };

            // Validate sort fields
            var allowedSortFields = new[] { "code", "name", "surcharge", "created_at", "updated_at" };
            if (!string.IsNullOrEmpty(request.SortBy) && !allowedSortFields.Contains(request.SortBy.ToLower()))
                errors["sortBy"] = new ValidationError { Msg = $"Trường sắp xếp không hợp lệ. Cho phép: {string.Join(", ", allowedSortFields)}", Path = "sortBy" };

            // Validate sort order
            if (!string.IsNullOrEmpty(request.SortOrder) && request.SortOrder.ToLower() != "asc" && request.SortOrder.ToLower() != "desc")
                errors["sortOrder"] = new ValidationError { Msg = "Thứ tự sắp xếp phải là 'asc' hoặc 'desc'", Path = "sortOrder" };

            // Validate search term length
            if (!string.IsNullOrEmpty(request.Search) && request.Search.Length > 100)
                errors["search"] = new ValidationError { Msg = "Từ khóa tìm kiếm không được vượt quá 100 ký tự", Path = "search" };

            // Validate code format
            if (!string.IsNullOrEmpty(request.Code) && request.Code.Length > 50)
                errors["code"] = new ValidationError { Msg = "Mã loại ghế không được vượt quá 50 ký tự", Path = "code" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidatePartnerAccessAsync(int partnerId, int userId)
        {
            // Kiểm tra user có role Partner hoặc Staff không
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && (u.UserType == "Partner" || u.UserType == "Staff" || u.UserType == "Marketing" || u.UserType == "Cashier"));

            if (user == null)
            {
                throw new UnauthorizedException("Chỉ tài khoản Partner hoặc Staff mới được sử dụng chức năng này");
            }

            // Nếu là Partner, kiểm tra partner thuộc về user này
            if (user.UserType == "Partner")
            {
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.UserId == userId && p.Status == "approved");

                if (partner == null)
                {
                    throw new UnauthorizedException("Partner không tồn tại hoặc không thuộc quyền quản lý của bạn");
                }

                if (!partner.IsActive)
                {
                    throw new UnauthorizedException("Tài khoản partner đã bị vô hiệu hóa");
                }
            }
            else if (user.UserType == "Staff" || user.UserType == "Marketing" || user.UserType == "Cashier")
            {
                // Nếu là Staff, kiểm tra employee thuộc về partner này
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee == null)
                {
                    throw new UnauthorizedException("Nhân viên không thuộc Partner này");
                }

                // Kiểm tra partner có tồn tại và active không
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.Status == "approved");

                if (partner == null || !partner.IsActive)
                {
                    throw new UnauthorizedException("Partner không tồn tại hoặc đã bị vô hiệu hóa");
                }
            }
        }

        private IQueryable<SeatType> ApplySeatTypeFilters(IQueryable<SeatType> query, GetSeatTypesRequest request)
        {
            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(st => st.Status == request.Status.Value);
            }

            // Filter by code (exact match, case insensitive)
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                query = query.Where(st => st.Code.ToLower() == request.Code.Trim().ToLower());
            }

            // Search by name or description
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(st =>
                    st.Name.ToLower().Contains(searchTerm) ||
                    (st.Description != null && st.Description.ToLower().Contains(searchTerm))
                );
            }

            // Filter by surcharge range
            if (request.MinSurcharge.HasValue)
            {
                query = query.Where(st => st.Surcharge >= request.MinSurcharge.Value);
            }

            if (request.MaxSurcharge.HasValue)
            {
                query = query.Where(st => st.Surcharge <= request.MaxSurcharge.Value);
            }

            return query;
        }

        private IQueryable<SeatType> ApplySeatTypeSorting(IQueryable<SeatType> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "code" => isAscending ? query.OrderBy(st => st.Code) : query.OrderByDescending(st => st.Code),
                "surcharge" => isAscending ? query.OrderBy(st => st.Surcharge) : query.OrderByDescending(st => st.Surcharge),
                "created_at" => isAscending ? query.OrderBy(st => st.CreatedAt) : query.OrderByDescending(st => st.CreatedAt),
                "updated_at" => isAscending ? query.OrderBy(st => st.UpdatedAt) : query.OrderByDescending(st => st.UpdatedAt),
                _ => isAscending ? query.OrderBy(st => st.Name) : query.OrderByDescending(st => st.Name) // default by name
            };
        }
        public async Task<SeatTypeDetailResponse> GetSeatTypeByIdAsync(int seatTypeId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Lấy seat type với validation thuộc về partner
            var seatType = await _context.SeatTypes
                .Where(st => st.Id == seatTypeId && st.PartnerId == partnerId)
                .Select(st => new
                {
                    SeatType = st,
                    TotalSeats = st.Seats.Count(),
                    ActiveSeats = st.Seats.Count(s => s.Status == "Available"),
                    InactiveSeats = st.Seats.Count(s => s.Status != "Available")
                })
                .FirstOrDefaultAsync();

            if (seatType == null)
            {
                throw new NotFoundException("Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            return new SeatTypeDetailResponse
            {
                Id = seatType.SeatType.Id,
                Code = seatType.SeatType.Code,
                Name = seatType.SeatType.Name,
                Surcharge = seatType.SeatType.Surcharge,
                Color = seatType.SeatType.Color,
                Description = seatType.SeatType.Description,
                Status = seatType.SeatType.Status,
                CreatedAt = seatType.SeatType.CreatedAt,
                UpdatedAt = seatType.SeatType.UpdatedAt,
                TotalSeats = seatType.TotalSeats,
                ActiveSeats = seatType.ActiveSeats,
                InactiveSeats = seatType.InactiveSeats
            };
        }
        public async Task<SeatTypeActionResponse> CreateSeatTypeAsync(CreateSeatTypeRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateCreateSeatTypeRequest(request);
            await ValidatePartnerAccessAsync(partnerId, userId);
            await ValidateSeatTypeCodeUniqueAsync(request.Code);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seatType = new SeatType
            {
                PartnerId = partnerId,
                Code = request.Code.Trim().ToUpper(),
                Name = request.Name.Trim(),
                Surcharge = request.Surcharge,
                Color = request.Color,
                Description = request.Description?.Trim(),
                Status = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.SeatTypes.Add(seatType);
                await _context.SaveChangesAsync();
                await _auditLogService.LogEntityChangeAsync(
                    action: "STAFF_CREATE_SEAT_TYPE",
                    tableName: "SeatType",
                    recordId: seatType.Id,
                    beforeData: null,
                    afterData: BuildSeatTypeSnapshot(seatType),
                    metadata: new { partnerId, userId });
            }
            catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
            {
                // Trường hợp race condition: 2 request song song vượt qua validate
                throw new ConflictException("code", "Mã loại ghế đã tồn tại trong hệ thống");
            }

            return new SeatTypeActionResponse
            {
                Id = seatType.Id,
                Code = seatType.Code,
                Name = seatType.Name,
                Surcharge = seatType.Surcharge,
                Color = seatType.Color,
                Description = seatType.Description,
                Status = seatType.Status,
                CreatedAt = seatType.CreatedAt,
                UpdatedAt = seatType.UpdatedAt,
                Message = "Tạo loại ghế thành công"
            };
        }

        public async Task<SeatTypeActionResponse> UpdateSeatTypeAsync(int seatTypeId, UpdateSeatTypeRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            if (seatTypeId <= 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["id"] = new ValidationError { Msg = "ID loại ghế phải lớn hơn 0", Path = "id" }
                });
            }

            ValidateUpdateSeatTypeRequest(request);
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seatType = await _context.SeatTypes
                .FirstOrDefaultAsync(st => st.Id == seatTypeId && st.PartnerId == partnerId);

            if (seatType == null)
            {
                throw new NotFoundException("Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            // Kiểm tra: Nếu đã có showtime sử dụng loại ghế này thì không được sửa bất kỳ thông tin nào
            var seatsWithThisType = await _context.Seats
                .Where(s => s.SeatTypeId == seatTypeId)
                .Select(s => s.ScreenId)
                .Distinct()
                .ToListAsync();

            if (seatsWithThisType.Any())
            {
                var hasShowtimes = await _context.Showtimes
                    .AnyAsync(st => seatsWithThisType.Contains(st.ScreenId));

                if (hasShowtimes)
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["showtimes"] = new ValidationError
                        {
                            Msg = "Không thể cập nhật thông tin loại ghế khi đã có suất chiếu sử dụng loại ghế này",
                            Path = "id"
                        }
                    });
                }
            }

            // Kiểm tra: Nếu đã có vé được bán cho ghế có loại ghế này thì không được sửa tên
            var seatIdsWithThisType = await _context.Seats
                .Where(s => s.SeatTypeId == seatTypeId)
                .Select(s => s.SeatId)
                .ToListAsync();

            if (seatIdsWithThisType.Any())
            {
                var hasSoldTickets = await _context.Tickets
                    .AnyAsync(t => seatIdsWithThisType.Contains(t.SeatId) && (t.Status == "VALID" || t.Status == "USED"));

                if (hasSoldTickets && seatType.Name.Trim().ToLower() != request.Name.Trim().ToLower())
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["name"] = new ValidationError
                        {
                            Msg = "Không thể thay đổi tên loại ghế khi đã có vé được bán cho ghế thuộc loại này",
                            Path = "name"
                        }
                    });
                }
            }

            // Validate nếu đang có ghế sử dụng thì không thể disable
            if (!request.Status && seatType.Status)
            {
                var activeSeatsCount = await _context.Seats
                    .CountAsync(s => s.SeatTypeId == seatTypeId && s.Status == "Available");

                if (activeSeatsCount > 0)
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["status"] = new ValidationError { Msg = $"Không thể vô hiệu hóa loại ghế đang có {activeSeatsCount} ghế đang hoạt động", Path = "status" }
                    });
                }
            }

            var beforeSnapshot = BuildSeatTypeSnapshot(seatType);

            seatType.Name = request.Name.Trim();
            seatType.Surcharge = request.Surcharge;
            seatType.Color = request.Color;
            seatType.Description = request.Description?.Trim();
            seatType.Status = request.Status;
            seatType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_UPDATE_SEAT_TYPE",
                tableName: "SeatType",
                recordId: seatType.Id,
                beforeData: beforeSnapshot,
                afterData: BuildSeatTypeSnapshot(seatType),
                metadata: new { partnerId, userId });

            return new SeatTypeActionResponse
            {
                Id = seatType.Id,
                Code = seatType.Code,
                Name = seatType.Name,
                Surcharge = seatType.Surcharge,
                Color = seatType.Color,
                Description = seatType.Description,
                Status = seatType.Status,
                CreatedAt = seatType.CreatedAt,
                UpdatedAt = seatType.UpdatedAt,
                Message = "Cập nhật loại ghế thành công"
            };
        }

        public async Task<SeatTypeActionResponse> DeleteSeatTypeAsync(int seatTypeId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            if (seatTypeId <= 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["id"] = new ValidationError { Msg = "ID loại ghế phải lớn hơn 0", Path = "id" }
                });
            }

            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seatType = await _context.SeatTypes
                .FirstOrDefaultAsync(st => st.Id == seatTypeId && st.PartnerId == partnerId);

            if (seatType == null)
            {
                throw new NotFoundException("Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            // Validate không thể xóa nếu đang có ghế sử dụng
            var activeSeatsCount = await _context.Seats
                .CountAsync(s => s.SeatTypeId == seatTypeId);

            if (activeSeatsCount > 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["delete"] = new ValidationError { Msg = $"Không thể xóa loại ghế đang có {activeSeatsCount} ghế sử dụng", Path = "id" }
                });
            }

            // SOFT DELETE - Chỉ cập nhật status
            var beforeSnapshot = BuildSeatTypeSnapshot(seatType);
            seatType.Status = false;
            seatType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_DELETE_SEAT_TYPE",
                tableName: "SeatType",
                recordId: seatType.Id,
                beforeData: beforeSnapshot,
                afterData: BuildSeatTypeSnapshot(seatType),
                metadata: new { partnerId, userId });

            return new SeatTypeActionResponse
            {
                Id = seatType.Id,
                Code = seatType.Code,
                Name = seatType.Name,
                Surcharge = seatType.Surcharge,
                Color = seatType.Color,
                Description = seatType.Description,
                Status = seatType.Status,
                CreatedAt = seatType.CreatedAt,
                UpdatedAt = seatType.UpdatedAt,
                Message = "Xóa loại ghế thành công"
            };
        }

        private static object BuildSeatTypeSnapshot(SeatType seatType) => new
        {
            seatType.Id,
            seatType.PartnerId,
            seatType.Code,
            seatType.Name,
            seatType.Surcharge,
            seatType.Color,
            seatType.Description,
            seatType.Status,
            seatType.CreatedAt,
            seatType.UpdatedAt
        };

        private void ValidateCreateSeatTypeRequest(CreateSeatTypeRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Code))
                errors["code"] = new ValidationError { Msg = "Mã loại ghế là bắt buộc", Path = "code" };
            else if (request.Code.Trim().Length > 50)
                errors["code"] = new ValidationError { Msg = "Mã loại ghế không được vượt quá 50 ký tự", Path = "code" };
            else if (!Regex.IsMatch(request.Code, @"^[A-Z0-9_]+$", RegexOptions.IgnoreCase))
                errors["code"] = new ValidationError { Msg = "Mã loại ghế chỉ được chứa chữ cái, số và dấu gạch dưới", Path = "code" };

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError { Msg = "Tên loại ghế là bắt buộc", Path = "name" };
            else if (request.Name.Trim().Length > 100)
                errors["name"] = new ValidationError { Msg = "Tên loại ghế không được vượt quá 100 ký tự", Path = "name" };

            if (request.Surcharge < 0)
                errors["surcharge"] = new ValidationError { Msg = "Phụ thu không thể âm", Path = "surcharge" };
            else if (request.Surcharge > 1000000)
                errors["surcharge"] = new ValidationError { Msg = "Phụ thu không được vượt quá 1,000,000", Path = "surcharge" };

            if (string.IsNullOrWhiteSpace(request.Color))
                errors["color"] = new ValidationError { Msg = "Màu sắc là bắt buộc", Path = "color" };
            else if (!Regex.IsMatch(request.Color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"))
                errors["color"] = new ValidationError { Msg = "Màu sắc phải là mã hex hợp lệ", Path = "color" };

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 255)
                errors["description"] = new ValidationError { Msg = "Mô tả không được vượt quá 255 ký tự", Path = "description" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateUpdateSeatTypeRequest(UpdateSeatTypeRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = new ValidationError { Msg = "Tên loại ghế là bắt buộc", Path = "name" };
            else if (request.Name.Trim().Length > 100)
                errors["name"] = new ValidationError { Msg = "Tên loại ghế không được vượt quá 100 ký tự", Path = "name" };

            if (request.Surcharge < 0)
                errors["surcharge"] = new ValidationError { Msg = "Phụ thu không thể âm", Path = "surcharge" };
            else if (request.Surcharge > 1000000)
                errors["surcharge"] = new ValidationError { Msg = "Phụ thu không được vượt quá 1,000,000", Path = "surcharge" };

            if (string.IsNullOrWhiteSpace(request.Color))
                errors["color"] = new ValidationError { Msg = "Màu sắc là bắt buộc", Path = "color" };
            else if (!Regex.IsMatch(request.Color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"))
                errors["color"] = new ValidationError { Msg = "Màu sắc phải là mã hex hợp lệ", Path = "color" };

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 255)
                errors["description"] = new ValidationError { Msg = "Mô tả không được vượt quá 255 ký tự", Path = "description" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateSeatTypeCodeUniqueAsync(string code)
        {
            var normalized = code.Trim().ToUpperInvariant();

            var exists = await _context.SeatTypes
                .AnyAsync(st => st.Code.ToUpper() == normalized);

            if (exists)
                throw new ConflictException("code", "Mã loại ghế đã tồn tại trong hệ thống");
        }
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
                return sqlEx.Number == 2627 || sqlEx.Number == 2601; // 2627: PK/Unique, 2601: Unique index
            return false;
        }
    }
}