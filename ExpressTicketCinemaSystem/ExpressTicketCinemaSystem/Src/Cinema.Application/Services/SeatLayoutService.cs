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

        public SeatLayoutService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<SeatLayoutResponse> GetSeatLayoutAsync(int screenId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);

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
                    TotalRows = 10, // Default
                    TotalColumns = 15, // Default
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
            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            ValidateCreateSeatLayoutRequest(request);

            // ==================== BUSINESS LOGIC SECTION ====================

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

                // B. Xóa tất cả seats cũ
                var existingSeats = _context.Seats.Where(s => s.ScreenId == screenId);
                _context.Seats.RemoveRange(existingSeats);

                // C. Tạo seats mới
                var newSeats = request.Seats.Select(s => new Seat
                {
                    ScreenId = screenId,
                    RowCode = s.Row.ToUpper(),
                    SeatNumber = s.Column,
                    SeatTypeId = s.SeatTypeId,
                    Status = s.Status
                }).ToList();

                _context.Seats.AddRange(newSeats);

                // D. Validate seat type belongs to partner
                await ValidateSeatTypesBelongToPartnerAsync(newSeats, partnerId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var blockedSeatsCount = newSeats.Count(s => s.Status == "Blocked");

                return new SeatLayoutActionResponse
                {
                    ScreenId = screenId,
                    TotalRows = request.TotalRows,
                    TotalColumns = request.TotalColumns,
                    TotalSeats = request.TotalRows * request.TotalColumns,
                    CreatedSeats = newSeats.Count,
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

            seat.SeatTypeId = request.SeatTypeId;
            seat.Status = request.Status;
            seat.SeatType = seatType;

            await _context.SaveChangesAsync();

            return new SeatActionResponse
            {
                SeatId = seat.SeatId,
                Row = seat.RowCode,
                Column = seat.SeatNumber,
                SeatTypeId = seat.SeatTypeId ?? 1,
                Status = seat.Status,
                Message = "Cập nhật ghế thành công",
                UpdatedAt = DateTime.UtcNow
            }; ; ;
        }

        public async Task<BulkSeatActionResponse> BulkUpdateSeatsAsync(int screenId, BulkUpdateSeatsRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidateScreenAccessAsync(screenId, partnerId, userId);
            ValidateBulkUpdateSeatsRequest(request);

            // ==================== BUSINESS LOGIC SECTION ====================

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
        private async Task ValidateScreenAccessAsync(int screenId, int partnerId, int userId)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validate screenId
            if (screenId <= 0)
            {
                errors["screenId"] = new ValidationError { Msg = "ID phòng chiếu phải lớn hơn 0", Path = "screenId" };
            }

            if (errors.Any())
                throw new ValidationException(errors);

            // Validate user có role Partner
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.UserType == "Partner");

            if (user == null)
            {
                throw new UnauthorizedException( "Chỉ tài khoản Partner mới được sử dụng chức năng này");
            }

            // Validate partner approved và active
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.UserId == userId && p.Status == "approved");

            if (partner == null)
            {
                throw new UnauthorizedException( "Partner không tồn tại hoặc không thuộc quyền quản lý của bạn");
            }

            if (!partner.IsActive)
            {
                throw new UnauthorizedException( "Tài khoản partner đã bị vô hiệu hóa");
            }

            // Validate screen thuộc về partner
            var screen = await _context.Screens
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ScreenId == screenId && s.Cinema.PartnerId == partnerId);

            if (screen == null)
            {
                throw new NotFoundException("Không tìm thấy phòng chiếu với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            if (!screen.IsActive)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["screen"] = new ValidationError { Msg = "Phòng chiếu đã bị vô hiệu hóa", Path = "screenId" }
                });
            }
        }

        private void ValidateCreateSeatLayoutRequest(CreateSeatLayoutRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

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

                // Check for duplicate positions
                var duplicatePositions = request.Seats
                    .GroupBy(s => new { Row = s.Row.ToUpper(), s.Column })
                    .Where(g => g.Count() > 1)
                    .Select(g => $"{g.Key.Row}{g.Key.Column}")
                    .ToList();

                if (duplicatePositions.Any())
                {
                    errors["seats"] = new ValidationError { Msg = $"Có vị trí ghế trùng lặp: {string.Join(", ", duplicatePositions)}", Path = "seats" };
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
    }
}