using Microsoft.AspNetCore.Mvc;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using System.IO;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Produces("application/json")]
    public class StaffController : ControllerBase
    {
        private readonly CinemaDbCoreContext _context;
        private readonly PartnerService _partnerService;
        private readonly ContractService _contractService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IScreenService _screenService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly ISeatLayoutService _seatLayoutService;
        private readonly IContractValidationService _contractValidationService;
        private readonly ICinemaService _cinemaService;
        private readonly IShowtimeService _showtimeService;
        private readonly IComboService _comboService;
        private readonly IEmployeeCinemaAssignmentService _employeeCinemaAssignmentService;

        public StaffController(PartnerService partnerService, ContractService contractService, IAzureBlobService azureBlobService, IScreenService screenService, ISeatTypeService seatTypeService, ISeatLayoutService seatLayoutService, CinemaDbCoreContext context, IContractValidationService contractValidationService, ICinemaService cinemaService, IShowtimeService showtimeService, IComboService comboService, IEmployeeCinemaAssignmentService employeeCinemaAssignmentService)
        {
            _partnerService = partnerService;
            _contractService = contractService;
            _azureBlobService = azureBlobService;
            _screenService = screenService;
            _seatTypeService = seatTypeService;
            _seatLayoutService = seatLayoutService;
            _context = context;
            _contractValidationService = contractValidationService;
            _cinemaService = cinemaService;
            _showtimeService = showtimeService;
            _comboService = comboService;
            _employeeCinemaAssignmentService = employeeCinemaAssignmentService;

        }
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(idClaim, out var id))
            {
                return id;
            }

            throw new UnauthorizedException("Token không hợp lệ hoặc không chứa ID người dùng.");
        }

        // Lấy PartnerId từ Employee với RoleType = "Staff"
        private async Task<int> GetCurrentPartnerId()
        {
            var userId = GetCurrentUserId();

            // Tìm employee từ userId với RoleType = "Staff"
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId && e.RoleType == "Staff" && e.IsActive);

            if (employee == null)
            {
                throw new UnauthorizedException("Không tìm thấy nhân viên Staff hoặc tài khoản không hoạt động.");
            }

            return employee.PartnerId;
        }

        // Lấy EmployeeId từ UserId
        private async Task<int> GetCurrentEmployeeId()
        {
            var userId = GetCurrentUserId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId && e.RoleType == "Staff" && e.IsActive);

            if (employee == null)
            {
                throw new UnauthorizedException("Không tìm thấy nhân viên Staff hoặc tài khoản không hoạt động.");
            }

            return employee.EmployeeId;
        }

        // Kiểm tra Staff có quyền truy cập cinema không
        private async Task ValidateStaffCinemaAccessAsync(int cinemaId)
        {
            var employeeId = await GetCurrentEmployeeId();
            var hasAccess = await _employeeCinemaAssignmentService.HasAccessToCinemaAsync(employeeId, cinemaId);

            if (!hasAccess)
            {
                throw new UnauthorizedException("Bạn không có quyền truy cập rạp này. Vui lòng liên hệ Partner để được phân quyền.");
            }
        }

        // Kiểm tra Staff có quyền truy cập screen thông qua cinema của screen đó
        private async Task ValidateStaffScreenAccessAsync(int screenId)
        {
            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            if (screen == null)
            {
                throw new NotFoundException("Không tìm thấy phòng chiếu");
            }

            await ValidateStaffCinemaAccessAsync(screen.CinemaId);
        }

        // Kiểm tra Staff có quyền truy cập showtime thông qua screen và cinema
        private async Task ValidateStaffShowtimeAccessAsync(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Screen)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu");
            }

            await ValidateStaffScreenAccessAsync(showtime.ScreenId);
        }



        // NOTE: Staff không thể tạo Cinema mới
        // Chỉ Partner mới có quyền tạo Cinema, sau đó phân quyền cho Staff
        // Endpoint CreateCinema đã được xóa khỏi StaffController
        /// <summary>
        /// Get cinema by ID
        /// </summary>
        [HttpGet("/staff/cinemas/{cinema_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCinemaById([FromRoute(Name = "cinema_id")] int cinemaId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffCinemaAccessAsync(cinemaId);

                var result = await _cinemaService.GetCinemaByIdAsync(cinemaId, partnerId, userId);

                var response = new SuccessResponse<CinemaResponse>
                {
                    Message = "Lấy thông tin rạp thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin rạp."
                });
            }
        }
        /// <summary>
        /// Get all cinemas for partner with filtering and pagination
        /// </summary>
        [HttpGet("/staff/cinemas")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedCinemasResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCinemas(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? city = null,
            [FromQuery] string? district = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "cinema_name",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Số trang phải lớn hơn 0",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["page"] = new ValidationError { Msg = "Số trang phải lớn hơn 0", Path = "page" }
                        }
                    });
                }

                if (limit < 1 || limit > 100)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Số lượng mỗi trang phải từ 1 đến 100",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["limit"] = new ValidationError { Msg = "Số lượng mỗi trang phải từ 1 đến 100", Path = "limit" }
                        }
                    });
                }

                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var employeeId = await GetCurrentEmployeeId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                // Lấy danh sách cinema được phân quyền
                var assignedCinemaIds = await _employeeCinemaAssignmentService.GetAssignedCinemaIdsAsync(employeeId);

                if (!assignedCinemaIds.Any())
                {
                    // Nếu không có cinema nào được phân quyền, trả về danh sách rỗng
                    return Ok(new SuccessResponse<PaginatedCinemasResponse>
                    {
                        Message = "Bạn chưa được phân quyền quản lý rạp nào. Vui lòng liên hệ Partner.",
                        Result = new PaginatedCinemasResponse
                        {
                            Cinemas = new List<CinemaResponse>(),
                            Pagination = new PaginationMetadata
                            {
                                CurrentPage = page,
                                PageSize = limit,
                                TotalCount = 0,
                                TotalPages = 0
                            }
                        }
                    });
                }

                // Lấy tất cả cinema từ Partner (không pagination trong service)
                var allCinemas = await _cinemaService.GetAllCinemasForStaffAsync(partnerId, userId,
                    city, district, isActive, search, sortBy, sortOrder);

                // Filter chỉ lấy các cinema được phân quyền
                var filteredCinemas = allCinemas.Where(c => assignedCinemaIds.Contains(c.CinemaId)).ToList();
                
                // Apply pagination
                var totalCount = filteredCinemas.Count;
                var paginatedCinemas = filteredCinemas
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                var result = new PaginatedCinemasResponse
                {
                    Cinemas = paginatedCinemas,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = page,
                        PageSize = limit,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
                    }
                };

                var response = new SuccessResponse<PaginatedCinemasResponse>
                {
                    Message = "Lấy danh sách rạp thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách rạp."
                });
            }
        }
        /// <summary>
        /// Update cinema
        /// </summary>
        [HttpPut("/staff/cinemas/{cinema_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCinema(
            [FromRoute(Name = "cinema_id")] int cinemaId,
            [FromBody] UpdateCinemaRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffCinemaAccessAsync(cinemaId);

                var result = await _cinemaService.UpdateCinemaAsync(cinemaId, request, partnerId, userId);

                var response = new SuccessResponse<CinemaResponse>
                {
                    Message = "Cập nhật rạp thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật rạp."
                });
            }
        }
        /// <summary>
        /// Delete cinema (Soft Delete)
        /// </summary>
        [HttpDelete("/staff/cinemas/{cinema_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCinema([FromRoute(Name = "cinema_id")] int cinemaId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffCinemaAccessAsync(cinemaId);

                var result = await _cinemaService.DeleteCinemaAsync(cinemaId, partnerId, userId);

                var response = new SuccessResponse<CinemaActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa rạp."
                });
            }
        }
        /// <summary>
        /// Create a new screen for partner's cinema
        /// </summary>
        [HttpPost("/staff/cinema/{cinema_id}/screens")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ScreenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateScreen(
            [FromRoute(Name = "cinema_id")] int cinemaId,
            [FromBody] CreateScreenRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffCinemaAccessAsync(cinemaId);

                var result = await _screenService.CreateScreenAsync(cinemaId, request, partnerId, userId);

                var response = new SuccessResponse<ScreenResponse>
                {
                    Message = "Tạo phòng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo phòng."
                });
            }
        }
        /// <summary>
        /// Get screen by ID for partner
        /// </summary>
        [HttpGet("/staff/screens/{screen_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ScreenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetScreenById([FromRoute(Name = "screen_id")] int screenId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _screenService.GetScreenByIdAsync(screenId, partnerId, userId);

                var response = new SuccessResponse<ScreenResponse>
                {
                    Message = "Lấy thông tin phòng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin phòng."
                });
            }
        }
        /// <summary>
        /// Get screens for partner's cinema with filtering and pagination
        /// </summary>
        [HttpGet("/staff/cinema/{cinema_id}/screens")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedScreensResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetScreens(
            [FromRoute(Name = "cinema_id")] int cinemaId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? screen_type = null,
            [FromQuery] bool? is_active = null,
            [FromQuery] string? sort_by = "screen_name",
            [FromQuery] string? sort_order = "asc")
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffCinemaAccessAsync(cinemaId);

                var result = await _screenService.GetScreensAsync(cinemaId, partnerId, userId, page, limit,
                    screen_type, is_active, sort_by, sort_order);

                var response = new SuccessResponse<PaginatedScreensResponse>
                {
                    Message = "Lấy danh sách phòng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách phòng."
                });
            }
        }
        /// <summary>
        /// Update screen for partner
        /// </summary>
        [HttpPut("/staff/screens/{screen_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ScreenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateScreen(
            [FromRoute(Name = "screen_id")] int screenId,
            [FromBody] UpdateScreenRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _screenService.UpdateScreenAsync(screenId, request, partnerId, userId);

                var response = new SuccessResponse<ScreenResponse>
                {
                    Message = "Cập nhật phòng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật phòng."
                });
            }
        }
        /// <summary>
        /// Delete screen for partner (Soft Delete)
        /// </summary>
        [HttpDelete("/staff/screens/{screen_id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ScreenActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteScreen([FromRoute(Name = "screen_id")] int screenId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _screenService.DeleteScreenAsync(screenId, partnerId, userId);

                var response = new SuccessResponse<ScreenActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa phòng."
                });
            }
        }
        /// <summary>
        /// Get all seat types with pagination and filtering
        /// </summary>
        [HttpGet("/staff/seat-types")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedSeatTypesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSeatTypes(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] bool? status = null,
            [FromQuery] string? code = null,
            [FromQuery] string? search = null,
            [FromQuery] decimal? minSurcharge = null,
            [FromQuery] decimal? maxSurcharge = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var request = new GetSeatTypesRequest
                {
                    Page = page,
                    Limit = limit,
                    Status = status,
                    Code = code,
                    Search = search,
                    MinSurcharge = minSurcharge,
                    MaxSurcharge = maxSurcharge,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _seatTypeService.GetSeatTypesAsync(request, partnerId, userId);

                var response = new SuccessResponse<PaginatedSeatTypesResponse>
                {
                    Message = "Lấy danh sách loại ghế thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách loại ghế."
                });
            }
        }
        /// <summary>
        /// Get seat type details by ID
        /// </summary>
        [HttpGet("/staff/seat-types/{id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatTypeDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSeatTypeById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _seatTypeService.GetSeatTypeByIdAsync(id, partnerId, userId);

                var response = new SuccessResponse<SeatTypeDetailResponse>
                {
                    Message = "Lấy thông tin loại ghế thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin loại ghế."
                });
            }
        }
        /// <summary>
        /// Create a new seat type
        /// </summary>
        [HttpPost("/staff/seat-types")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatTypeActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateSeatType([FromBody] CreateSeatTypeRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _seatTypeService.CreateSeatTypeAsync(request, partnerId, userId);

                var response = new SuccessResponse<SeatTypeActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo loại ghế."
                });
            }
        }
        /// <summary>
        /// Update seat type by ID
        /// </summary>
        [HttpPut("/staff/seat-types/{id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatTypeActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSeatType(int id, [FromBody] UpdateSeatTypeRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _seatTypeService.UpdateSeatTypeAsync(id, request, partnerId, userId);

                var response = new SuccessResponse<SeatTypeActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật loại ghế."
                });
            }
        }
        /// <summary>
        /// Delete seat type by ID (Soft Delete)
        /// </summary>
        [HttpDelete("/staff/seat-types/{id}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatTypeActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSeatType(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _seatTypeService.DeleteSeatTypeAsync(id, partnerId, userId);

                var response = new SuccessResponse<SeatTypeActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa loại ghế."
                });
            }
        }
        /// <summary>
        /// Get seat layout for screen
        /// </summary>
        [HttpGet("/staff/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatLayoutResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSeatLayout(int screenId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.GetSeatLayoutAsync(screenId, partnerId, userId);

                var response = new SuccessResponse<SeatLayoutResponse>
                {
                    Message = result.SeatMap.HasLayout
                        ? "Lấy thông tin layout ghế thành công"
                        : "Chưa có layout ghế cho phòng này",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin layout ghế."
                });
            }
        }
        /// <summary>
        /// Get available seat types for screen
        /// </summary>
        [HttpGet("/staff/screens/{screenId}/seat-types")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ScreenSeatTypesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetScreenSeatTypes(int screenId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _seatLayoutService.GetScreenSeatTypesAsync(screenId, partnerId, userId);

                var response = new SuccessResponse<ScreenSeatTypesResponse>
                {
                    Message = "Lấy danh sách loại ghế thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách loại ghế."
                });
            }
        }
        /// <summary>
        /// Create new seat layout for screen
        /// </summary>
        [HttpPost("/staff/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatLayoutActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateSeatLayout(int screenId, [FromBody] CreateSeatLayoutRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.CreateOrUpdateSeatLayoutAsync(screenId, request, partnerId, userId);

                var response = new SuccessResponse<SeatLayoutActionResponse>
                {
                    Message = "Tạo layout ghế thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo layout ghế."
                });
            }
        }
        /// <summary>
        /// Update existing seat layout for screen
        /// </summary>
        [HttpPut("/staff/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatLayoutActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSeatLayout(int screenId, [FromBody] CreateSeatLayoutRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.CreateOrUpdateSeatLayoutAsync(screenId, request, partnerId, userId);

                var response = new SuccessResponse<SeatLayoutActionResponse>
                {
                    Message = "Cập nhật layout ghế thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật layout ghế."
                });
            }
        }
        /// <summary>
        /// Update individual seat
        /// </summary>
        [HttpPut("/staff/screens/{screenId}/seat-layout/{seatId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSeat(int screenId, int seatId, [FromBody] UpdateSeatRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.UpdateSeatAsync(screenId, seatId, request, partnerId, userId);

                var response = new SuccessResponse<SeatActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật ghế."
                });
            }
        }
        /// <summary>
        /// Bulk update multiple seats
        /// </summary>
        [HttpPost("/staff/screens/{screenId}/seat-layout/bulk")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<BulkSeatActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BulkUpdateSeats(int screenId, [FromBody] BulkUpdateSeatsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.BulkUpdateSeatsAsync(screenId, request, partnerId, userId);

                var response = new SuccessResponse<BulkSeatActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var errorMessage = ex.Errors.Values.FirstOrDefault()?.Msg;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "Xác thực thất bại";
                }

                return Unauthorized(new ValidationErrorResponse
                {
                    Message = errorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật hàng loạt ghế."
                });
            }
        }
        /// <summary>
        /// Delete entire seat layout for screen
        /// </summary>
        [HttpDelete("/staff/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatLayoutActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSeatLayout(int screenId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.DeleteSeatLayoutAsync(screenId, partnerId, userId);

                var response = new SuccessResponse<SeatLayoutActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa layout ghế."
                });
            }
        }

        /// <summary>
        /// Delete individual seat
        /// </summary>
        [HttpDelete("/staff/screens/{screenId}/seat-layout/{seatId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<SeatActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSeat(int screenId, int seatId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.DeleteSeatAsync(screenId, seatId, partnerId, userId);

                var response = new SuccessResponse<SeatActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa ghế."
                });
            }
        }

        /// <summary>
        /// Bulk delete multiple seats
        /// </summary>
        [HttpDelete("/staff/screens/{screenId}/seat-layout/bulk")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<BulkSeatActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BulkDeleteSeats(int screenId, [FromBody] BulkDeleteSeatsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                await ValidateStaffScreenAccessAsync(screenId);

                var result = await _seatLayoutService.BulkDeleteSeatsAsync(screenId, request, partnerId, userId);

                var response = new SuccessResponse<BulkSeatActionResponse>
                {
                    Message = result.Message,
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa hàng loạt ghế."
                });
            }
        }


        /// <summary>
        /// Create a new showtime for partners
        /// </summary>
        [HttpPost("/staff/showtimes")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateShowtime([FromBody] PartnerShowtimeCreateRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await ValidateStaffScreenAccessAsync(request.ScreenId);
                var result = await _showtimeService.CreatePartnerShowtimeAsync(partnerId, request);

                var response = new SuccessResponse<PartnerShowtimeCreateResponse>
                {
                    Message = "Tạo showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo suất chiếu."
                });
            }
        }

        /// <summary>
        /// Update showtime for partner
        /// </summary>
        [HttpPut("/staff/showtimes/{showtimeId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateShowtime(int showtimeId, [FromBody] PartnerShowtimeCreateRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await ValidateStaffShowtimeAccessAsync(showtimeId);
                await ValidateStaffScreenAccessAsync(request.ScreenId);
                var result = await _showtimeService.UpdatePartnerShowtimeAsync(partnerId, showtimeId, request);

                var response = new SuccessResponse<PartnerShowtimeCreateResponse>
                {
                    Message = "Cập nhật showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật suất chiếu."
                });
            }
        }

        /// <summary>
        /// Soft delete showtime for partners
        /// </summary>
        [HttpDelete("/staff/showtimes/{showtimeId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteShowtime(int showtimeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await ValidateStaffShowtimeAccessAsync(showtimeId);
                var result = await _showtimeService.DeletePartnerShowtimeAsync(partnerId, showtimeId);

                var response = new SuccessResponse<PartnerShowtimeCreateResponse>
                {
                    Message = "Xóa showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa suất chiếu."
                });
            }
        }

        /// <summary>
        /// Get showtime by ID
        /// </summary>
        [HttpGet("/staff/showtimes/{showtimeId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeById(int showtimeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await ValidateStaffShowtimeAccessAsync(showtimeId);
                var result = await _showtimeService.GetPartnerShowtimeByIdAsync(partnerId, showtimeId);

                var response = new SuccessResponse<PartnerShowtimeDetailResponse>
                {
                    Message = "Lấy thông tin showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin suất chiếu."
                });
            }
        }

        /// <summary>
        /// Get all showtimes for partners with pagination and filtering.
        /// </summary>
        [HttpGet("/staff/showtimes")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllShowtimes(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? movie_id = null,
            [FromQuery] string? cinema_id = null,
            [FromQuery] string? screen_id = null,
            [FromQuery] string? date = null,
            [FromQuery] string? status = null,
            [FromQuery] string sort_by = "start_time",
            [FromQuery] string sort_order = "asc")
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var employeeId = await GetCurrentEmployeeId();

                // Lấy danh sách cinema được phân quyền
                var assignedCinemaIds = await _employeeCinemaAssignmentService.GetAssignedCinemaIdsAsync(employeeId);

                if (!assignedCinemaIds.Any())
                {
                    return Ok(new SuccessResponse<PartnerShowtimeListResponse>
                    {
                        Message = "Bạn chưa được phân quyền quản lý rạp nào.",
                        Result = new PartnerShowtimeListResponse
                        {
                            Showtimes = new List<PartnerShowtimeListItem>(),
                            Total = 0,
                            Page = page,
                            Limit = limit,
                            TotalPages = 0
                        }
                    });
                }

                // Nếu có filter cinema_id, kiểm tra quyền
                if (!string.IsNullOrEmpty(cinema_id) && int.TryParse(cinema_id, out var cinemaId))
                {
                    if (!assignedCinemaIds.Contains(cinemaId))
                    {
                        throw new UnauthorizedException("Bạn không có quyền truy cập rạp này.");
                    }
                }

                var request = new PartnerShowtimeQueryRequest
                {
                    Page = page,
                    Limit = limit,
                    MovieId = movie_id,
                    CinemaId = cinema_id,
                    ScreenId = screen_id,
                    Date = date,
                    Status = status,
                    SortBy = sort_by,
                    SortOrder = sort_order
                };

                // Lấy danh sách screenId từ các cinema được phân quyền
                var assignedScreenIds = await _context.Screens
                    .Where(s => assignedCinemaIds.Contains(s.CinemaId))
                    .Select(s => s.ScreenId)
                    .ToListAsync();

                // Nếu có filter screen_id, kiểm tra quyền
                if (!string.IsNullOrEmpty(screen_id) && int.TryParse(screen_id, out var screenId))
                {
                    if (!assignedScreenIds.Contains(screenId))
                    {
                        throw new UnauthorizedException("Bạn không có quyền truy cập phòng chiếu này.");
                    }
                }

                var result = await _showtimeService.GetPartnerShowtimesAsync(partnerId, request);

                // Filter chỉ lấy showtime của các screen được phân quyền
                var allShowtimes = result.Showtimes
                    .Where(s => !string.IsNullOrEmpty(s.ScreenId) && 
                                int.TryParse(s.ScreenId, out var screenId) && 
                                assignedScreenIds.Contains(screenId))
                    .ToList();

                // Apply pagination sau khi filter
                var totalCount = allShowtimes.Count;
                var paginatedShowtimes = allShowtimes
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                result.Showtimes = paginatedShowtimes;
                result.Total = totalCount;
                result.Page = page;
                result.Limit = limit;
                result.TotalPages = (int)Math.Ceiling(totalCount / (double)limit);

                var response = new SuccessResponse<PartnerShowtimeListResponse>
                {
                    Message = "Lấy danh sách showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách suất chiếu."
                });
            }
        }
        // ==================== COMBO/SERVICE MANAGEMENT (READ ONLY) ====================
        // NOTE: Staff chỉ có quyền xem combo, không thể tạo/sửa/xóa
        // Partner có toàn quyền CRUD combo (quản lý cho cả chuỗi rạp)

        /// <summary>Get combo by id</summary>
        [HttpGet("/staff/services/{serviceId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<ServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetServiceById(int serviceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var result = await _comboService.GetByIdAsync(partnerId, userId, serviceId);

                return Ok(new SuccessResponse<ServiceResponse>
                {
                    Message = "Lấy thông tin combo thành công",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin combo." });
            }
        }

        /// <summary>Get combos (paging/filter/sort)</summary>
        [HttpGet("/staff/services")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedServicesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetServices(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? is_available = null,
            [FromQuery] string sort_by = "created_at",
            [FromQuery] string sort_order = "desc")
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                var query = new GetServicesQuery
                {
                    Page = page,
                    Limit = limit,
                    Search = search,
                    IsAvailable = is_available,
                    SortBy = sort_by,
                    SortOrder = sort_order
                };

                var result = await _comboService.GetListAsync(partnerId, userId, query);

                return Ok(new SuccessResponse<PaginatedServicesResponse>
                {
                    Message = "Lấy danh sách combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = first, Errors = ex.Errors });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách combo." });
            }
        }

        // NOTE: Update và Delete combo đã bị xóa khỏi StaffController
        // Chỉ Partner mới có quyền tạo, sửa, xóa combo (quản lý cho cả chuỗi rạp)

        /// <summary>
        /// Get partner's booking orders from their cinemas with filtering
        /// </summary>
        /// <param name="cinemaId">Filter by cinema ID (optional, if null gets all partner's cinemas)</param>
        /// <param name="status">Filter by booking status</param>
        /// <param name="paymentStatus">Filter by payment status</param>
        /// <param name="fromDate">Filter from booking date</param>
        /// <param name="toDate">Filter to booking date</param>
        /// <param name="customerId">Filter by customer ID</param>
        /// <param name="customerEmail">Search by customer email</param>
        /// <param name="customerPhone">Search by customer phone</param>
        /// <param name="bookingCode">Search by booking code</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="sortBy">Sort by field</param>
        /// <param name="sortOrder">Sort order (asc/desc)</param>
        [HttpGet("/staff/bookings")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerBookingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerBookings(
            [FromQuery] int? cinemaId,
            [FromQuery] string? status,
            [FromQuery] string? paymentStatus,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? customerId,
            [FromQuery] string? customerEmail,
            [FromQuery] string? customerPhone,
            [FromQuery] string? bookingCode,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "booking_time",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var userId = GetCurrentUserId();
                var request = new GetPartnerBookingsRequest
                {
                    CinemaId = cinemaId,
                    Status = status,
                    PaymentStatus = paymentStatus,
                    FromDate = fromDate,
                    ToDate = toDate,
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    BookingCode = bookingCode,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _partnerService.GetPartnerBookingsAsync(userId, request);

                return Ok(new SuccessResponse<PartnerBookingsResponse>
                {
                    Message = "Lấy danh sách đơn hàng thành công.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = "Xác thực thất bại",
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Lỗi khi lấy danh sách đơn hàng: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Get booking detail by booking ID
        /// Partner can only view bookings from their own cinemas
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Booking detail</returns>
        [HttpGet("/staff/bookings/{bookingId}")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerBookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerBookingDetail(int bookingId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _partnerService.GetPartnerBookingDetailAsync(userId, bookingId);

                return Ok(new SuccessResponse<PartnerBookingDetailResponse>
                {
                    Message = "Lấy chi tiết đơn hàng thành công.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = "Xác thực thất bại",
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Lỗi khi lấy chi tiết đơn hàng: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Get Staff's booking statistics from assigned cinemas only
        /// Staff can only view statistics from cinemas they are assigned to manage
        /// </summary>
        /// <param name="fromDate">Filter from booking date (ISO format). If null, defaults to 30 days ago</param>
        /// <param name="toDate">Filter to booking date (ISO format). If null, defaults to today</param>
        /// <param name="cinemaId">Filter by cinema ID (must be one of the assigned cinemas)</param>
        /// <param name="groupBy">Group by time period: "day", "week", "month", "year" (default: "day")</param>
        /// <param name="includeComparison">Include comparison with previous period (default: false)</param>
        /// <param name="topLimit">Limit for top items (default: 10)</param>
        /// <param name="page">Page number for paginated lists (default: 1)</param>
        /// <param name="pageSize">Page size for paginated lists (default: 20, max: 100)</param>
        /// <returns>Booking statistics from assigned cinemas only</returns>
        [HttpGet("/staff/bookings/statistics")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerBookingStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStaffBookingStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? cinemaId,
            [FromQuery] string groupBy = "day",
            [FromQuery] bool includeComparison = false,
            [FromQuery] int topLimit = 10,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                var employeeId = await GetCurrentEmployeeId();
                var partnerId = await GetCurrentPartnerId();

                // Lấy danh sách cinema được phân quyền
                var assignedCinemaIds = await _employeeCinemaAssignmentService.GetAssignedCinemaIdsAsync(employeeId);

                if (!assignedCinemaIds.Any())
                {
                    // Nếu không có cinema nào được phân quyền, trả về statistics rỗng
                    return Ok(new SuccessResponse<PartnerBookingStatisticsResponse>
                    {
                        Message = "Bạn chưa được phân quyền quản lý rạp nào. Vui lòng liên hệ Partner.",
                        Result = new PartnerBookingStatisticsResponse()
                    });
                }

                // Nếu có filter cinemaId, kiểm tra quyền truy cập
                if (cinemaId.HasValue)
                {
                    if (!assignedCinemaIds.Contains(cinemaId.Value))
                    {
                        throw new UnauthorizedException("Bạn không có quyền xem thống kê của rạp này.");
                    }
                }

                var request = new GetPartnerBookingStatisticsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    CinemaId = cinemaId,
                    GroupBy = groupBy,
                    IncludeComparison = includeComparison,
                    TopLimit = topLimit,
                    Page = page,
                    PageSize = pageSize
                };

                // Gọi method mới cho Staff (filter theo assigned cinemas)
                var result = await _partnerService.GetStaffBookingStatisticsAsync(userId, employeeId, assignedCinemaIds, request);

                return Ok(new SuccessResponse<PartnerBookingStatisticsResponse>
                {
                    Message = "Lấy thống kê đơn hàng thành công.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors?.Values.FirstOrDefault()?.Msg ?? ex.Message ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors ?? new Dictionary<string, ValidationError>()
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Lỗi khi lấy thống kê đơn hàng: " + ex.Message
                });
            }
        }
    }
}
