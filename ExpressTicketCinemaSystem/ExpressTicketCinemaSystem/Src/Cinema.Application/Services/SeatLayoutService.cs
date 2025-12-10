using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Text.RegularExpressions;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class SeatLayoutService : ISeatLayoutService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmployeeCinemaAssignmentService _employeeCinemaAssignmentService;

        public SeatLayoutService(CinemaDbCoreContext context, IAuditLogService auditLogService, IEmployeeCinemaAssignmentService employeeCinemaAssignmentService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _employeeCinemaAssignmentService = employeeCinemaAssignmentService;
        }

        public async Task<SeatLayoutResponse> GetSeatLayoutAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            var screen = await ValidateScreenAccessAsync(screenId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Lấy SeatMap
            var seatMap = await _context.SeatMaps
                .FirstOrDefaultAsync(sm => sm.ScreenId == screenId);

            // Lấy danh sách ghế
            var seats = await _context.Seats
                .Where(s => s.ScreenId == screenId)
                .Include(s => s.SeatType)
                .Select(s => new SeatResponse
                {
                    SeatId = s.SeatId,
                    Row = s.RowCode,
                    Column = s.SeatNumber,
                    SeatName = s.SeatName, 
                    SeatTypeId = s.SeatTypeId ?? 1,
                    SeatTypeCode = s.SeatType.Code,
                    SeatTypeName = s.SeatType.Name,
                    SeatTypeColor = s.SeatType.Color,
                    Status = s.Status
                })
                .ToListAsync();

            // Lấy available seat types của partner
            var seatTypes = await _context.SeatTypes
                .Where(st => st.PartnerId == partnerId && st.Status)
                .Select(st => new SeatTypeResponse
                {
                    Id = st.Id,
                    Code = st.Code,
                    Name = st.Name,
                    Surcharge = st.Surcharge,
                    Color = st.Color,
                    Description = st.Description,
                    Status = st.Status
                })
                .ToListAsync();

            var seatMapResponse = seatMap != null
                ? new SeatMapResponse
                {
                    SeatMapId = seatMap.SeatMapId,
                    ScreenId = seatMap.ScreenId,
                    TotalRows = seatMap.TotalRows,
                    TotalColumns = seatMap.TotalColumns,
                    UpdatedAt = seatMap.UpdatedAt,
                    HasLayout = true
                }
                : new SeatMapResponse
                {
                    ScreenId = screenId,
                    TotalRows = screen.SeatRows ?? 10,
                    TotalColumns = screen.SeatColumns ?? 15,
                    HasLayout = false
                };

            return new SeatLayoutResponse
            {
                SeatMap = seatMapResponse,
                Seats = seats,
                AvailableSeatTypes = seatTypes
            };
        }

        public async Task<ScreenSeatTypesResponse> GetScreenSeatTypesAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seatTypes = await _context.SeatTypes
                .Where(st => st.PartnerId == partnerId && st.Status)
                .Select(st => new SeatTypeResponse
                {
                    Id = st.Id,
                    Code = st.Code,
                    Name = st.Name,
                    Surcharge = st.Surcharge,
                    Color = st.Color,
                    Description = st.Description,
                    Status = st.Status
                })
                .ToListAsync();

            return new ScreenSeatTypesResponse
            {
                SeatTypes = seatTypes
            };
        }

        public async Task<SeatLayoutActionResponse> CreateOrUpdateSeatLayoutAsync(int screenId, CreateSeatLayoutRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            var screen = await ValidateScreenAccessAsync(screenId, partnerId, userId);
            await ValidateCreateSeatLayoutRequestAsync(screenId, request, partnerId, screen);

            // ==================== BUSINESS LOGIC SECTION ====================
            var beforeSnapshot = await BuildSeatLayoutSnapshotAsync(screenId);
            var hasExistingLayout = beforeSnapshot.SeatMap != null || beforeSnapshot.Seats.Count > 0;

            // Kiểm tra: Nếu đã có layout và đang update, không cho phép nếu có showtime nào
            if (hasExistingLayout)
            {
                await ValidateNoShowtimesForScreenAsync(screenId);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // A. Tạo hoặc cập nhật SeatMap
                var seatMap = await _context.SeatMaps
                    .FirstOrDefaultAsync(sm => sm.ScreenId == screenId);

                if (seatMap == null)
                {
                    seatMap = new SeatMap
                    {
                        ScreenId = screenId,
                        TotalRows = request.TotalRows,
                        TotalColumns = request.TotalColumns,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.SeatMaps.Add(seatMap);
                }
                else
                {
                    seatMap.TotalRows = request.TotalRows;
                    seatMap.TotalColumns = request.TotalColumns;
                    seatMap.UpdatedAt = DateTime.UtcNow;
                }

                // B. Lấy danh sách ghế hiện tại
                var existingSeats = await _context.Seats
                    .Where(s => s.ScreenId == screenId)
                    .ToDictionaryAsync(s => $"{s.RowCode.ToUpper()}_{s.SeatNumber}");

                // C. Tạo dictionary từ request
                var newSeatsDict = request.Seats.ToDictionary(
                    s => $"{s.Row.ToUpper()}_{s.Column}",
                    s => new Seat
                    {
                        ScreenId = screenId,
                        RowCode = s.Row.ToUpper(),
                        SeatNumber = s.Column,
                        SeatName = s.SeatName,
                        SeatTypeId = s.SeatTypeId,
                        Status = s.Status
                    });

                // D. Xác định các thao tác cần thực hiện
                var seatsToRemove = existingSeats.Keys.Except(newSeatsDict.Keys).ToList();
                var seatsToAdd = newSeatsDict.Keys.Except(existingSeats.Keys).ToList();
                var seatsToUpdate = existingSeats.Keys.Intersect(newSeatsDict.Keys).ToList();

                // E. Xóa ghế không còn trong layout mới
                if (seatsToRemove.Any())
                {
                    var removeSeatIds = seatsToRemove.Select(key => existingSeats[key].SeatId).ToList();
                    var seatsToDelete = await _context.Seats
                        .Where(s => removeSeatIds.Contains(s.SeatId))
                        .ToListAsync();
                    _context.Seats.RemoveRange(seatsToDelete);
                }

                // F. Thêm ghế mới
                if (seatsToAdd.Any())
                {
                    var seatsToInsert = seatsToAdd.Select(key => newSeatsDict[key]).ToList();
                    _context.Seats.AddRange(seatsToInsert);
                }

                // G. Cập nhật ghế hiện có
                if (seatsToUpdate.Any())
                {
                    foreach (var key in seatsToUpdate)
                    {
                        var existingSeat = existingSeats[key];
                        var newSeatData = newSeatsDict[key];

                        existingSeat.SeatTypeId = newSeatData.SeatTypeId;
                        existingSeat.Status = newSeatData.Status;
                        existingSeat.SeatName = newSeatData.SeatName;
                    }
                }

                // H. Validate seat types thuộc về partner
                await ValidateSeatTypesBelongToPartnerAsync(newSeatsDict.Values.ToList(), partnerId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var blockedSeatsCount = newSeatsDict.Values.Count(s => s.Status == "Blocked");
                var afterSnapshot = await BuildSeatLayoutSnapshotAsync(screenId);
                var layoutAction = hasExistingLayout ? "STAFF_UPDATE_SEAT_LAYOUT" : "STAFF_CREATE_SEAT_LAYOUT";

                await _auditLogService.LogEntityChangeAsync(
                    action: layoutAction,
                    tableName: "SeatLayout",
                    recordId: screenId,
                    beforeData: beforeSnapshot,
                    afterData: afterSnapshot,
                    metadata: new
                    {
                        partnerId,
                        userId,
                        createdSeats = seatsToAdd.Count,
                        updatedSeats = seatsToUpdate.Count,
                        removedSeats = seatsToRemove.Count,
                        blockedSeats = blockedSeatsCount
                    });

                return new SeatLayoutActionResponse
                {
                    ScreenId = screenId,
                    TotalRows = request.TotalRows,
                    TotalColumns = request.TotalColumns,
                    TotalSeats = newSeatsDict.Count,
                    CreatedSeats = seatsToAdd.Count,
                    UpdatedSeats = seatsToUpdate.Count,
                    //DeletedSeats = seatsToRemove.Count,
                    BlockedSeats = blockedSeatsCount,
                    Message = seatMap.SeatMapId == 0 ? "Tạo layout ghế thành công" : "Cập nhật layout ghế thành công",
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SeatActionResponse> UpdateSeatAsync(int screenId, int seatId, UpdateSeatRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            if (seatId <= 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["seatId"] = new ValidationError { Msg = "ID ghế phải lớn hơn 0", Path = "seatId" }
                });
            }

            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            ValidateUpdateSeatRequest(request);

            // Kiểm tra: Không cho phép update ghế nếu screen đã có showtime
            await ValidateNoShowtimesForScreenAsync(screenId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seat = await _context.Seats
                .Include(s => s.SeatType)
                .FirstOrDefaultAsync(s => s.SeatId == seatId && s.ScreenId == screenId);

            if (seat == null)
            {
                throw new NotFoundException("Không tìm thấy ghế với ID này trong phòng chiếu");
            }

            // Validate seat type belongs to partner và active
            var seatType = await _context.SeatTypes
                .FirstOrDefaultAsync(st => st.Id == request.SeatTypeId && st.PartnerId == partnerId && st.Status);

            if (seatType == null)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["seatTypeId"] = new ValidationError { Msg = "Loại ghế không tồn tại, không hoạt động hoặc không thuộc quyền quản lý của bạn", Path = "seatTypeId" }
                });
            }

            var beforeSeatSnapshot = BuildSeatSnapshot(seat);

            seat.SeatTypeId = request.SeatTypeId;
            seat.Status = request.Status;
            seat.SeatName = request.SeatName;
            seat.SeatType = seatType;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_UPDATE_SEAT_LAYOUT_ITEM",
                tableName: "SeatLayout",
                recordId: seat.SeatId,
                beforeData: beforeSeatSnapshot,
                afterData: BuildSeatSnapshot(seat),
                metadata: new { screenId, partnerId, userId });

            return new SeatActionResponse
            {
                SeatId = seat.SeatId,
                Row = seat.RowCode,
                Column = seat.SeatNumber,
                SeatName = seat.SeatName,
                SeatTypeId = seat.SeatTypeId ?? 1,
                Status = seat.Status,
                Message = "Cập nhật ghế thành công",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<BulkSeatActionResponse> BulkUpdateSeatsAsync(int screenId, BulkUpdateSeatsRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            ValidateBulkUpdateSeatsRequest(request);

            // Kiểm tra: Không cho phép bulk update ghế nếu screen đã có showtime
            await ValidateNoShowtimesForScreenAsync(screenId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var targetSeatIds = request.SeatUpdates
                .Where(su => su.SeatId > 0)
                .Select(su => su.SeatId)
                .Distinct()
                .ToList();
            var beforeSeatSnapshots = await _context.Seats
                .Where(s => s.ScreenId == screenId && targetSeatIds.Contains(s.SeatId))
                .ToListAsync();
            var beforeSnapshotPayload = beforeSeatSnapshots.Select(BuildSeatSnapshot).ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var results = new List<SeatActionResult>();
                var successCount = 0;
                var failedCount = 0;

                // Lấy tất cả seat types ACTIVE của partner
                var partnerSeatTypeIds = await _context.SeatTypes
                    .Where(st => st.PartnerId == partnerId && st.Status)
                    .Select(st => st.Id)
                    .ToListAsync();

                foreach (var seatUpdate in request.SeatUpdates)
                {
                    try
                    {
                        // Validate seat ID
                        if (seatUpdate.SeatId <= 0)
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatUpdate.SeatId,
                                Success = false,
                                Message = "ID ghế không hợp lệ",
                                Error = "Invalid seat ID"
                            });
                            failedCount++;
                            continue;
                        }

                        // VALIDATE: Seat thuộc về screen này
                        var seat = await _context.Seats
                            .FirstOrDefaultAsync(s => s.SeatId == seatUpdate.SeatId && s.ScreenId == screenId);

                        if (seat == null)
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatUpdate.SeatId,
                                Success = false,
                                Message = "Không tìm thấy ghế trong phòng này",
                                Error = "Seat not found in screen"
                            });
                            failedCount++;
                            continue;
                        }

                        // VALIDATE: Seat type active và thuộc partner
                        if (!partnerSeatTypeIds.Contains(seatUpdate.SeatTypeId))
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatUpdate.SeatId,
                                Success = false,
                                Message = "Loại ghế không tồn tại hoặc đã bị vô hiệu hóa",
                                Error = "Invalid or inactive seat type"
                            });
                            failedCount++;
                            continue;
                        }

                        // VALIDATE: Seat status
                        var validStatuses = new[] { "Available", "Blocked", "Maintenance" };
                        if (!validStatuses.Contains(seatUpdate.Status))
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatUpdate.SeatId,
                                Success = false,
                                Message = "Trạng thái ghế không hợp lệ",
                                Error = "Invalid seat status"
                            });
                            failedCount++;
                            continue;
                        }

                        seat.SeatTypeId = seatUpdate.SeatTypeId;
                        seat.Status = seatUpdate.Status;
                        seat.SeatName = seatUpdate.SeatName;

                        results.Add(new SeatActionResult
                        {
                            SeatId = seatUpdate.SeatId,
                            Success = true,
                            Message = "Cập nhật thành công"
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new SeatActionResult
                        {
                            SeatId = seatUpdate.SeatId,
                            Success = false,
                            Message = "Lỗi khi cập nhật",
                            Error = ex.Message
                        });
                        failedCount++;
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var afterSeatEntities = await _context.Seats
                        .Where(s => s.ScreenId == screenId && targetSeatIds.Contains(s.SeatId))
                        .ToListAsync();
                    var afterSnapshotPayload = afterSeatEntities.Select(BuildSeatSnapshot).ToList();

                    await _auditLogService.LogEntityChangeAsync(
                        action: "STAFF_CREATE_SEAT_LAYOUT_BULK",
                        tableName: "SeatLayout",
                        recordId: screenId,
                        beforeData: beforeSnapshotPayload,
                        afterData: afterSnapshotPayload,
                        metadata: new { partnerId, userId, successCount, failedCount });
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return new BulkSeatActionResponse
                {
                    TotalProcessed = request.SeatUpdates.Count,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    Results = results,
                    Message = $"Xử lý {successCount}/{request.SeatUpdates.Count} ghế thành công"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==================== VALIDATION METHODS ====================
        private async Task<Screen> ValidateScreenAccessAsync(int screenId, int partnerId, int userId)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validate screenId
            if (screenId <= 0)
            {
                errors["screenId"] = new ValidationError { Msg = "ID phòng chiếu phải lớn hơn 0", Path = "screenId" };
            }

            if (errors.Any())
                throw new ValidationException(errors);

            // Validate user có role Partner hoặc Staff
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && (u.UserType == "Partner" || u.UserType == "Staff" || u.UserType == "Marketing" || u.UserType == "Cashier"));

            if (user == null)
            {
                throw new UnauthorizedException("Chỉ tài khoản Partner hoặc Staff mới được sử dụng chức năng này");
            }

            Partner? partner = null;

            // Nếu là Partner, validate trực tiếp
            if (user.UserType == "Partner")
            {
                partner = await _context.Partners
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
            // Nếu là Staff, validate qua Employee
            else if (user.UserType == "Staff" || user.UserType == "Marketing" || user.UserType == "Cashier")
            {
                var employee = await _context.Employees
                    .Include(e => e.Partner)
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.PartnerId == partnerId && e.IsActive);

                if (employee == null || employee.Partner == null)
                {
                    throw new UnauthorizedException("Nhân viên không thuộc Partner này");
                }

                partner = employee.Partner;

                if (partner.Status != "approved" || !partner.IsActive)
                {
                    throw new UnauthorizedException("Partner không tồn tại hoặc chưa được duyệt");
                }
            }

            // Validate screen thuộc về partner
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId && s.Cinema.PartnerId == partnerId);

            if (screen == null)
            {
                throw new NotFoundException("Không tìm thấy phòng chiếu với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            // Nếu là Staff, kiểm tra có được phân quyền rạp của phòng này không
            if (user.UserType == "Staff" || user.UserType == "Marketing" || user.UserType == "Cashier")
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

            if (!screen.IsActive)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["screen"] = new ValidationError { Msg = "Phòng chiếu đã bị vô hiệu hóa", Path = "screenId" }
                });
            }

            return screen;
        }

        private async Task ValidateCreateSeatLayoutRequestAsync(int screenId, CreateSeatLayoutRequest request, int partnerId, Screen screen)
        {
            var errors = new Dictionary<string, ValidationError>();

            // ✅ VALIDATE: Total rows không vượt quá screen configuration
            if (request.TotalRows > (screen.SeatRows ?? 50))
            {
                errors["totalRows"] = new ValidationError
                {
                    Msg = $"Số hàng không được vượt quá {screen.SeatRows} (theo cấu hình phòng)",
                    Path = "totalRows"
                };
            }

            // ✅ VALIDATE: Total columns không vượt quá screen configuration
            if (request.TotalColumns > (screen.SeatColumns ?? 30))
            {
                errors["totalColumns"] = new ValidationError
                {
                    Msg = $"Số cột không được vượt quá {screen.SeatColumns} (theo cấu hình phòng)",
                    Path = "totalColumns"
                };
            }

            // Validate total rows
            if (request.TotalRows < 1 || request.TotalRows > 50)
                errors["totalRows"] = new ValidationError { Msg = "Số hàng phải từ 1 đến 50", Path = "totalRows" };

            // Validate total columns
            if (request.TotalColumns < 1 || request.TotalColumns > 30)
                errors["totalColumns"] = new ValidationError { Msg = "Số cột phải từ 1 đến 30", Path = "totalColumns" };

            // Validate seats list
            if (request.Seats == null || !request.Seats.Any())
                errors["seats"] = new ValidationError { Msg = "Danh sách ghế không được để trống", Path = "seats" };

            // Validate individual seats
            if (request.Seats != null)
            {
                for (int i = 0; i < request.Seats.Count; i++)
                {
                    var seat = request.Seats[i];

                    // Validate row
                    if (string.IsNullOrWhiteSpace(seat.Row))
                        errors[$"seats[{i}].row"] = new ValidationError { Msg = "Hàng không được để trống", Path = $"seats[{i}].row" };
                    else if (seat.Row.Length > 2)
                        errors[$"seats[{i}].row"] = new ValidationError { Msg = "Hàng không được vượt quá 2 ký tự", Path = $"seats[{i}].row" };
                    else if (!Regex.IsMatch(seat.Row, @"^[A-Z]+$", RegexOptions.IgnoreCase))
                        errors[$"seats[{i}].row"] = new ValidationError { Msg = "Hàng chỉ được chứa chữ cái", Path = $"seats[{i}].row" };

                    // ✅ VALIDATE: Row number không vượt quá total rows
                    var rowNumber = ConvertRowToNumber(seat.Row);
                    if (rowNumber > request.TotalRows)
                    {
                        errors[$"seats[{i}].row"] = new ValidationError
                        {
                            Msg = $"Hàng {seat.Row} vượt quá số hàng tối đa ({request.TotalRows})",
                            Path = $"seats[{i}].row"
                        };
                    }

                    // Validate column
                    if (seat.Column < 1 || seat.Column > request.TotalColumns)
                        errors[$"seats[{i}].column"] = new ValidationError { Msg = $"Số cột phải từ 1 đến {request.TotalColumns}", Path = $"seats[{i}].column" };

                    // Validate seat type
                    if (seat.SeatTypeId <= 0)
                        errors[$"seats[{i}].seatTypeId"] = new ValidationError { Msg = "Loại ghế không hợp lệ", Path = $"seats[{i}].seatTypeId" };

                    // Validate status
                    var validStatuses = new[] { "Available", "Blocked", "Maintenance" };
                    if (!validStatuses.Contains(seat.Status))
                        errors[$"seats[{i}].status"] = new ValidationError { Msg = $"Trạng thái phải là: {string.Join(", ", validStatuses)}", Path = $"seats[{i}].status" };
                }

                // Check for duplicate positions trong request
                var duplicatePositions = request.Seats
                    .GroupBy(s => new { Row = s.Row.ToUpper(), s.Column })
                    .Where(g => g.Count() > 1)
                    .Select(g => $"{g.Key.Row}{g.Key.Column}")
                    .ToList();

                if (duplicatePositions.Any())
                {
                    errors["seats"] = new ValidationError { Msg = $"Có vị trí ghế trùng lặp trong request: {string.Join(", ", duplicatePositions)}", Path = "seats" };
                }

                // ✅ VALIDATE: Tổng số ghế không vượt quá capacity của phòng
                var totalSeats = request.Seats.Count;
                if (totalSeats > (screen.Capacity ?? 300))
                {
                    errors["seats"] = new ValidationError
                    {
                        Msg = $"Tổng số ghế ({totalSeats}) vượt quá sức chứa tối đa của phòng ({screen.Capacity})",
                        Path = "seats"
                    };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateUpdateSeatRequest(UpdateSeatRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (request.SeatTypeId <= 0)
                errors["seatTypeId"] = new ValidationError { Msg = "Loại ghế không hợp lệ", Path = "seatTypeId" };

            var validStatuses = new[] { "Available", "Blocked", "Maintenance" };
            if (!validStatuses.Contains(request.Status))
                errors["status"] = new ValidationError { Msg = $"Trạng thái phải là: {string.Join(", ", validStatuses)}", Path = "status" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateBulkUpdateSeatsRequest(BulkUpdateSeatsRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (request.SeatUpdates == null || !request.SeatUpdates.Any())
                errors["seatUpdates"] = new ValidationError { Msg = "Danh sách cập nhật ghế không được để trống", Path = "seatUpdates" };

            if (request.SeatUpdates != null && request.SeatUpdates.Count > 100)
                errors["seatUpdates"] = new ValidationError { Msg = "Chỉ được phép cập nhật tối đa 100 ghế cùng lúc", Path = "seatUpdates" };

            // Validate individual seat updates
            if (request.SeatUpdates != null)
            {
                for (int i = 0; i < request.SeatUpdates.Count; i++)
                {
                    var seatUpdate = request.SeatUpdates[i];

                    if (seatUpdate.SeatId <= 0)
                        errors[$"seatUpdates[{i}].seatId"] = new ValidationError { Msg = "ID ghế không hợp lệ", Path = $"seatUpdates[{i}].seatId" };

                    if (seatUpdate.SeatTypeId <= 0)
                        errors[$"seatUpdates[{i}].seatTypeId"] = new ValidationError { Msg = "Loại ghế không hợp lệ", Path = $"seatUpdates[{i}].seatTypeId" };

                    var validStatuses = new[] { "Available", "Blocked", "Maintenance" };
                    if (!validStatuses.Contains(seatUpdate.Status))
                        errors[$"seatUpdates[{i}].status"] = new ValidationError { Msg = $"Trạng thái phải là: {string.Join(", ", validStatuses)}", Path = $"seatUpdates[{i}].status" };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateSeatTypesBelongToPartnerAsync(List<Seat> seats, int partnerId)
        {
            if (seats?.Any() != true) return;

            var seatTypeIds = seats.Select(s => s.SeatTypeId)
                                  .Where(id => id > 0)
                                  .Distinct()
                                  .ToList();

            if (!seatTypeIds.Any()) return;

            var validSeatTypeIds = await _context.SeatTypes
              .Where(st => st.PartnerId == partnerId && st.Status)
              .Select(st => (int?)st.Id)
              .ToListAsync();

            var invalidSeatTypeIds = seatTypeIds.Except(validSeatTypeIds).ToList();

            if (invalidSeatTypeIds.Any())
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["seatTypeId"] = new ValidationError
                    {
                        Msg = $"Loại ghế không hợp lệ: {string.Join(", ", invalidSeatTypeIds)}",
                        Path = "seats"
                    }
                });
            }
        }

        // ✅ THÊM METHOD: Chuyển đổi chữ cái thành số (A=1, B=2, ..., Z=26, AA=27, AB=28, ...)
        private int ConvertRowToNumber(string row)
        {
            if (string.IsNullOrEmpty(row)) return 0;

            row = row.ToUpper();
            int result = 0;

            foreach (char c in row)
            {
                result = result * 26 + (c - 'A' + 1);
            }

            return result;
        }
        // THÊM VÀO CUỐI CLASS SeatLayoutService

        public async Task<SeatLayoutActionResponse> DeleteSeatLayoutAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            await ValidateCanDeleteSeatsAsync(screenId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var beforeSnapshot = await BuildSeatLayoutSnapshotAsync(screenId);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Lấy thông tin trước khi xóa để response
                var seats = await _context.Seats
                    .Where(s => s.ScreenId == screenId)
                    .ToListAsync();
                var seatMap = await _context.SeatMaps
                    .FirstOrDefaultAsync(sm => sm.ScreenId == screenId);

                int totalSeats = seats.Count;
                int totalRows = seatMap?.TotalRows ?? 0;
                int totalColumns = seatMap?.TotalColumns ?? 0;

                // 2. Xóa tất cả seats
                if (seats.Any())
                {
                    _context.Seats.RemoveRange(seats);
                }

                // 3. Xóa seat map
                if (seatMap != null)
                {
                    _context.SeatMaps.Remove(seatMap);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var afterSnapshot = await BuildSeatLayoutSnapshotAsync(screenId);
                await _auditLogService.LogEntityChangeAsync(
                    action: "STAFF_DELETE_SEAT_LAYOUT",
                    tableName: "SeatLayout",
                    recordId: screenId,
                    beforeData: beforeSnapshot,
                    afterData: afterSnapshot,
                    metadata: new { partnerId, userId, totalSeats });

                return new SeatLayoutActionResponse
                {
                    ScreenId = screenId,
                    TotalRows = totalRows,
                    TotalColumns = totalColumns,
                    TotalSeats = totalSeats,
                    Message = $"Xóa layout thành công: {totalSeats} ghế",
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SeatActionResponse> DeleteSeatAsync(int screenId, int seatId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            if (seatId <= 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["seatId"] = new ValidationError { Msg = "ID ghế phải lớn hơn 0", Path = "seatId" }
                });
            }

            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            await ValidateCanDeleteSeatsAsync(screenId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var seat = await _context.Seats
                .FirstOrDefaultAsync(s => s.SeatId == seatId && s.ScreenId == screenId);

            if (seat == null)
            {
                throw new NotFoundException("Không tìm thấy ghế với ID này trong phòng chiếu");
            }

            // Lưu thông tin để response
            var row = seat.RowCode;
            var column = seat.SeatNumber;
            var beforeSeatSnapshot = BuildSeatSnapshot(seat);

            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "STAFF_DELETE_SEAT_LAYOUT_ITEM",
                tableName: "SeatLayout",
                recordId: seatId,
                beforeData: beforeSeatSnapshot,
                afterData: new { SeatId = seatId, Deleted = true },
                metadata: new { screenId, partnerId, userId });

            return new SeatActionResponse
            {
                SeatId = seatId,
                Row = row,
                Column = column,
                Message = $"Xóa ghế {row}{column} thành công",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<BulkSeatActionResponse> BulkDeleteSeatsAsync(int screenId, BulkDeleteSeatsRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            await ValidateCanDeleteSeatsAsync(screenId);
            ValidateBulkDeleteSeatsRequest(request);

            // ==================== BUSINESS LOGIC SECTION ====================
            var requestedSeatIds = request.SeatIds.Distinct().ToList();
            var seatsBeforeDelete = await _context.Seats
                .Where(s => s.ScreenId == screenId && requestedSeatIds.Contains(s.SeatId))
                .ToListAsync();
            var beforeSnapshot = seatsBeforeDelete.Select(BuildSeatSnapshot).ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var results = new List<SeatActionResult>();
                var successCount = 0;
                var failedCount = 0;

                // Lấy tất cả seats tồn tại
                var existingSeats = await _context.Seats
                    .Where(s => s.ScreenId == screenId && request.SeatIds.Contains(s.SeatId))
                    .ToDictionaryAsync(s => s.SeatId);

                foreach (var seatId in request.SeatIds)
                {
                    try
                    {
                        // Validate seat ID
                        if (seatId <= 0)
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatId,
                                Success = false,
                                Message = "ID ghế không hợp lệ",
                                Error = "Invalid seat ID"
                            });
                            failedCount++;
                            continue;
                        }

                        // Kiểm tra seat tồn tại
                        if (!existingSeats.ContainsKey(seatId))
                        {
                            results.Add(new SeatActionResult
                            {
                                SeatId = seatId,
                                Success = false,
                                Message = "Không tìm thấy ghế trong phòng này",
                                Error = "Seat not found in screen"
                            });
                            failedCount++;
                            continue;
                        }

                        var seat = existingSeats[seatId];
                        _context.Seats.Remove(seat);

                        results.Add(new SeatActionResult
                        {
                            SeatId = seatId,
                            Success = true,
                            Message = "Xóa thành công"
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new SeatActionResult
                        {
                            SeatId = seatId,
                            Success = false,
                            Message = "Lỗi khi xóa",
                            Error = ex.Message
                        });
                        failedCount++;
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _auditLogService.LogEntityChangeAsync(
                        action: "STAFF_DELETE_SEAT_LAYOUT_BULK",
                        tableName: "SeatLayout",
                        recordId: screenId,
                        beforeData: beforeSnapshot,
                        afterData: new
                        {
                            DeletedSeatIds = beforeSnapshot.Select(s => s.SeatId).ToList()
                        },
                        metadata: new { partnerId, userId, successCount, failedCount });
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                return new BulkSeatActionResponse
                {
                    TotalProcessed = request.SeatIds.Count,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    Results = results,
                    Message = $"Xóa {successCount}/{request.SeatIds.Count} ghế thành công"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==================== VALIDATION METHODS ====================

        private async Task ValidateCanDeleteSeatsAsync(int screenId)
        {
            // ✅ CHECK: Chỉ cho phép xóa nếu CHƯA có showtime nào được tạo
            var hasShowtimes = await _context.Showtimes
                .AnyAsync(st => st.ScreenId == screenId);

            if (hasShowtimes)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["showtimes"] = new ValidationError
                    {
                        Msg = "Không thể xóa ghế khi đã có lịch chiếu được tạo",
                        Path = "screenId"
                    }
                });
            }
        }

        private async Task ValidateNoShowtimesForScreenAsync(int screenId)
        {
            // ✅ CHECK: Không cho phép cập nhật/thay đổi sơ đồ ghế nếu đã có showtime
            var hasShowtimes = await _context.Showtimes
                .AnyAsync(st => st.ScreenId == screenId);

            if (hasShowtimes)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["showtimes"] = new ValidationError
                    {
                        Msg = "Không thể cập nhật hoặc thay đổi sơ đồ ghế khi đã có suất chiếu được tạo",
                        Path = "screenId"
                    }
                });
            }
        }

        private void ValidateBulkDeleteSeatsRequest(BulkDeleteSeatsRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (request.SeatIds == null || !request.SeatIds.Any())
                errors["seatIds"] = new ValidationError { Msg = "Danh sách seatIds là bắt buộc", Path = "seatIds" };

            if (request.SeatIds != null && request.SeatIds.Count > 100)
                errors["seatIds"] = new ValidationError { Msg = "Chỉ được phép xóa tối đa 100 ghế cùng lúc", Path = "seatIds" };

            // Validate individual seat IDs
            if (request.SeatIds != null)
            {
                for (int i = 0; i < request.SeatIds.Count; i++)
                {
                    if (request.SeatIds[i] <= 0)
                        errors[$"seatIds[{i}]"] = new ValidationError { Msg = "ID ghế phải lớn hơn 0", Path = $"seatIds[{i}]" };
                }

                // Check duplicate seat IDs
                var duplicateSeatIds = request.SeatIds
                    .GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateSeatIds.Any())
                {
                    errors["seatIds"] = new ValidationError { Msg = $"Có seatId trùng lặp: {string.Join(", ", duplicateSeatIds)}", Path = "seatIds" };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task<SeatLayoutSnapshot> BuildSeatLayoutSnapshotAsync(int screenId)
        {
            var seatMap = await _context.SeatMaps
                .AsNoTracking()
                .FirstOrDefaultAsync(sm => sm.ScreenId == screenId);

            var seatEntities = await _context.Seats
                .AsNoTracking()
                .Where(s => s.ScreenId == screenId)
                .ToListAsync();
            var seats = seatEntities
                .Select(BuildSeatSnapshot)
                .ToList();

            return new SeatLayoutSnapshot
            {
                ScreenId = screenId,
                SeatMap = seatMap == null
                    ? null
                    : new SeatMapSnapshot
                    {
                        SeatMapId = seatMap.SeatMapId,
                        TotalRows = seatMap.TotalRows,
                        TotalColumns = seatMap.TotalColumns,
                        UpdatedAt = seatMap.UpdatedAt
                    },
                Seats = seats
            };
        }

        private static SeatSnapshot BuildSeatSnapshot(Seat seat)
        {
            return new SeatSnapshot
            {
                SeatId = seat.SeatId,
                Row = seat.RowCode,
                Column = seat.SeatNumber,
                SeatName = seat.SeatName,
                SeatTypeId = seat.SeatTypeId,
                Status = seat.Status
            };
        }

        private sealed class SeatLayoutSnapshot
        {
            public int ScreenId { get; set; }
            public SeatMapSnapshot? SeatMap { get; set; }
            public List<SeatSnapshot> Seats { get; set; } = new();
        }

        private sealed class SeatMapSnapshot
        {
            public int SeatMapId { get; set; }
            public int TotalRows { get; set; }
            public int TotalColumns { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        private sealed class SeatSnapshot
        {
            public int SeatId { get; set; }
            public string? Row { get; set; }
            public int Column { get; set; }
            public string? SeatName { get; set; }
            public int? SeatTypeId { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}