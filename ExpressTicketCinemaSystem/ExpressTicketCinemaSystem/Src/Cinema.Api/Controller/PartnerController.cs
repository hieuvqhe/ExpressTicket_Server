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
using ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Staff.Responses;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/partners")]
    [Produces("application/json")]
    public class PartnersController : ControllerBase
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
        private readonly EmployeeManagementService _employeeManagementService;
        private readonly IEmployeeCinemaAssignmentService _employeeCinemaAssignmentService;
        private readonly IPermissionService _permissionService;
        private readonly IStaffService _staffService;
        private readonly ILogger<PartnersController> _logger;

        public PartnersController(PartnerService partnerService, ContractService contractService, IAzureBlobService azureBlobService, IScreenService screenService, ISeatTypeService seatTypeService, ISeatLayoutService seatLayoutService, CinemaDbCoreContext context, IContractValidationService contractValidationService, ICinemaService cinemaService, IShowtimeService showtimeService, IComboService comboService, EmployeeManagementService employeeManagementService, IEmployeeCinemaAssignmentService employeeCinemaAssignmentService, IPermissionService permissionService, IStaffService staffService, ILogger<PartnersController> logger)
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
            _employeeManagementService = employeeManagementService;
            _employeeCinemaAssignmentService = employeeCinemaAssignmentService;
            _permissionService = permissionService;
            _staffService = staffService;
            _logger = logger;
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

        // THÊM METHOD MỚI: Lấy PartnerId từ UserId
        private async Task<int> GetCurrentPartnerId()
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Nếu là Partner, lấy trực tiếp
            if (userRole == "Partner")
            {
                var partner = await _contractService.GetPartnerByUserId(userId);
                return partner.PartnerId;
            }

            // Nếu là Staff, lấy từ Employee
            if (userRole == "Staff")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.RoleType == "Staff" && e.IsActive);

                if (employee == null)
                    throw new UnauthorizedException("Không tìm thấy nhân viên Staff hoặc tài khoản không hoạt động.");

                return employee.PartnerId;
            }

            throw new UnauthorizedException("Role không hợp lệ.");
        }

        // Lấy EmployeeId từ UserId (chỉ cho Staff)
        private async Task<int?> GetCurrentEmployeeId()
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Staff")
                return null;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId && e.RoleType == "Staff" && e.IsActive);

            return employee?.EmployeeId;
        }

        // Kiểm tra Staff có quyền truy cập cinema không
        private async Task ValidateStaffCinemaAccessAsync(int cinemaId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Staff")
                return; // Partner bypass

            var employeeId = await GetCurrentEmployeeId();
            if (!employeeId.HasValue)
                throw new UnauthorizedException("Không tìm thấy nhân viên Staff.");

            var hasAccess = await _employeeCinemaAssignmentService.HasAccessToCinemaAsync(employeeId.Value, cinemaId);
            if (!hasAccess)
                throw new UnauthorizedException("Bạn không có quyền truy cập rạp này. Vui lòng liên hệ Partner để được phân quyền.");
        }

        // Kiểm tra Staff có quyền truy cập screen thông qua cinema của screen đó
        private async Task ValidateStaffScreenAccessAsync(int screenId)
        {
            var screen = await _context.Screens
                .FirstOrDefaultAsync(s => s.ScreenId == screenId);

            if (screen == null)
                throw new NotFoundException("Không tìm thấy phòng chiếu");

            await ValidateStaffCinemaAccessAsync(screen.CinemaId);
        }

        // Kiểm tra Staff có quyền truy cập showtime thông qua screen và cinema
        private async Task ValidateStaffShowtimeAccessAsync(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Screen)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
                throw new NotFoundException("Không tìm thấy suất chiếu");

            await ValidateStaffScreenAccessAsync(showtime.ScreenId);
        }

        /// <summary>
        /// Register a new partner
        /// </summary>
        [HttpPost("/partners/register")]
        [AuditAction("PARTNER_REGISTER", "Partner", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<PartnerRegisterResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] PartnerRegisterRequest request)
        {
            try
            {
                var result = await _partnerService.RegisterPartnerAsync(request);

                var response = new SuccessResponse<PartnerRegisterResponse>
                {
                    Message = "Đăng ký đối tác thành công. Hồ sơ của bạn đang chờ xét duyệt.",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Dữ liệu bị xung đột",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký đối tác. Email: {Email}, PartnerName: {PartnerName}", 
                    request?.Email, request?.PartnerName);
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đăng ký đối tác.",
                });
            }
        }

        /// <summary>
        /// Partner get profile
        /// </summary>
        [HttpGet("/partners/profile")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _partnerService.GetPartnerProfileAsync(userId);

                var response = new SuccessResponse<PartnerProfileResponse>
                {
                    Message = "Lấy thông tin partner thành công",
                    Result = result
                };
                return Ok(response);
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin partner.",
                });
            }
        }
        /// <summary>
        /// Update partner profile
        /// </summary>
        [HttpPatch("/partners/profile")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProfile([FromBody] PartnerUpdateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _partnerService.UpdatePartnerProfileAsync(userId, request);

                var response = new SuccessResponse<PartnerProfileResponse>
                {
                    Message = "Cập nhật thông tin partner thành công",
                    Result = result
                };
                return Ok(response);
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
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Dữ liệu bị xung đột",
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
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật thông tin partner.",
                });
            }
        }
        /// <summary>
        /// Upload signed PDF contract (replaces the original PDF sent by manager)
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <param name="request">Signed contract PDF upload request</param>
        /// <returns>Updated contract details</returns>
        [HttpPost("/partners/contracts/{id}/upload-signature")]
        [AuditAction("PARTNER_UPLOAD_SIGNATURE", "Contract", recordIdRouteKey: "id", includeRequestBody: false)]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadSignature(int id, [FromBody] UploadSignatureRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var result = await _contractService.UploadPartnerSignatureAsync(id, partnerId, request);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Upload PDF hợp đồng đã ký thành công",
                    Result = result
                };
                return Ok(response);
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
                    Message = "Đã xảy ra lỗi hệ thống khi upload PDF hợp đồng đã ký."
                });
            }
        }
        /// <summary>
        /// Get all contracts for current partner with filtering and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="status">Filter by contract status (draft, pending, active, expired)</param>
        /// <param name="contractType">Filter by contract type (partnership, service, standard, premium)</param>
        /// <param name="search">Search term for contract number or title</param>
        /// <param name="sortBy">Field to sort by (contract_number, title, start_date, end_date, commission_rate, status, created_at)</param>
        /// <param name="sortOrder">Sort order (asc, desc)</param>
        /// <returns>Paginated list of contracts</returns>
        [HttpGet("/partners/contracts")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedContractsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerContracts(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? contractType = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var result = await _contractService.GetPartnerContractsAsync(
                    partnerId, page, limit, status, contractType, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedContractsResponse>
                {
                    Message = "Lấy danh sách hợp đồng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách hợp đồng."
                });
            }
        }
        /// <summary>
        /// Get contract details by ID for current partner
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <returns>Contract details</returns>
        [HttpGet("/partners/contracts/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContractById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _contractService.GetPartnerContractByIdAsync(id, userId);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Lấy thông tin hợp đồng thành công",
                    Result = result
                };
                return Ok(response);
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin hợp đồng."
                });
            }
        }
        /// <summary>
        /// (Partner) Tạo SAS URL để upload file PDF hợp đồng đã ký
        /// </summary>
        /// <param name="request">Chứa tên file PDF, ví dụ: "signed-contract.pdf"</param>
        /// <returns>SAS URL (for uploading) and Blob URL (for saving)</returns>
        [HttpPost("/partners/contracts/generate-signature-upload-sas")]
        [AuditAction("PARTNER_GENERATE_SIGNATURE_SAS", "Contract", includeRequestBody: true)]
        [Authorize(Roles = "Partner")] // Đảm bảo chỉ partner mới được gọi
        [ProducesResponseType(typeof(SuccessResponse<GeneratePdfUploadUrlResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        [Consumes("application/json")]
        public async Task<IActionResult> GenerateSignatureUploadUrl([FromBody] GeneratePdfUploadUrlRequest request)
        {
            // === VALIDATION ===
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Tên file là bắt buộc",
                    Errors = new Dictionary<string, ValidationError>
                    {
                        ["fileName"] = new ValidationError { Msg = "Tên file không được để trống" }
                    }
                });
            }

            // Chỉ chấp nhận file PDF hợp đồng đã ký
            var allowedExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(request.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Định dạng file không hợp lệ",
                    Errors = new Dictionary<string, ValidationError>
                    {
                        ["fileName"] = new ValidationError { Msg = "Chỉ chấp nhận file PDF hợp đồng đã ký" }
                    }
                });
            }

            // === BUSINESS LOGIC ===
            try
            {
                var result = await _azureBlobService.GeneratePdfUploadUrlAsync(request.FileName);

                var response = new SuccessResponse<GeneratePdfUploadUrlResponse>
                {
                    Message = "Tạo SAS URL cho chữ ký thành công",
                    Result = result
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo URL upload chữ ký."
                });
            }
        }

        // ==================== CINEMA MANAGEMENT ====================

        /// <summary>
        /// Create a new cinema
        /// </summary>
        [HttpPost("/partners/cinemas")]
        [AuditAction("PARTNER_CREATE_CINEMA", "Cinema", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCinema([FromBody] CreateCinemaRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _cinemaService.CreateCinemaAsync(request, partnerId, userId);

                var response = new SuccessResponse<CinemaResponse>
                {
                    Message = "Tạo rạp thành công",
                    Result = result
                };

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu đầu vào không hợp lệ";
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
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo rạp."
                });
            }
        }

        /// <summary>
        /// Get cinema by ID (View-only)
        /// </summary>
        [HttpGet("/partners/cinemas/{cinema_id}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("CINEMA_READ")]
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
        [HttpGet("/partners/cinemas")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("CINEMA_READ")]
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
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _cinemaService.GetCinemasAsync(partnerId, userId, page, limit,
                    city, district, isActive, search, sortBy, sortOrder);

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
        /// Update cinema information
        /// </summary>
        [HttpPut("/partners/cinemas/{cinema_id}")]
        [AuditAction("PARTNER_UPDATE_CINEMA", "Cinema", recordIdRouteKey: "cinema_id", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("CINEMA_UPDATE")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
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
                    Message = "Cập nhật thông tin rạp thành công",
                    Result = result
                };

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu đầu vào không hợp lệ";
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
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật rạp."
                });
            }
        }

        /// <summary>
        /// Delete/deactivate cinema
        /// </summary>
        [HttpDelete("/partners/cinemas/{cinema_id}")]
        [AuditAction("PARTNER_DELETE_CINEMA", "Cinema", recordIdRouteKey: "cinema_id", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("CINEMA_DELETE")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
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

                await _cinemaService.DeleteCinemaAsync(cinemaId, partnerId, userId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa rạp thành công",
                    Result = null
                };

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu đầu vào không hợp lệ";
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
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa rạp."
                });
            }
        }

        // ==================== SCREEN MANAGEMENT ====================

        /// <summary>
        /// Get screen by ID for partner (View-only)
        /// </summary>
        [HttpGet("/partners/screens/{screen_id}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SCREEN_READ")]
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
        [HttpGet("/partners/cinema/{cinema_id}/screens")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SCREEN_READ")]
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
        /// Create a new screen for cinema
        /// </summary>
        [HttpPost("/partners/cinema/{cinema_id}/screens")]
        [AuditAction("PARTNER_CREATE_SCREEN", "Screen", recordIdRouteKey: "cinema_id", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SCREEN_CREATE")]
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
        /// Update screen
        /// </summary>
        [HttpPut("/partners/screens/{screen_id}")]
        [AuditAction("PARTNER_UPDATE_SCREEN", "Screen", recordIdRouteKey: "screen_id", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SCREEN_UPDATE")]
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
        /// Delete screen (Soft Delete)
        /// </summary>
        [HttpDelete("/partners/screens/{screen_id}")]
        [AuditAction("PARTNER_DELETE_SCREEN", "Screen", recordIdRouteKey: "screen_id", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SCREEN_DELETE")]
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
        [HttpGet("/partners/seat-types")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_TYPE_READ")]
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
        [HttpGet("/partners/seat-types/{id}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_TYPE_READ")]
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
        [HttpPost("/partners/seat-types")]
        [AuditAction("PARTNER_CREATE_SEAT_TYPE", "SeatType", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_TYPE_CREATE")]
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
        [HttpPut("/partners/seat-types/{id}")]
        [AuditAction("PARTNER_UPDATE_SEAT_TYPE", "SeatType", recordIdRouteKey: "id", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_TYPE_UPDATE")]
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
        [HttpDelete("/partners/seat-types/{id}")]
        [AuditAction("PARTNER_DELETE_SEAT_TYPE", "SeatType", recordIdRouteKey: "id", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_TYPE_DELETE")]
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
        [HttpGet("/partners/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_READ")]
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
        [HttpGet("/partners/screens/{screenId}/seat-types")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_READ")]
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
        /// Create seat layout for screen
        /// </summary>
        [HttpPost("/partners/screens/{screenId}/seat-layout")]
        [AuditAction("PARTNER_CREATE_SEAT_LAYOUT", "SeatLayout", recordIdRouteKey: "screenId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_CREATE")]
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
        /// Update seat layout for screen
        /// </summary>
        [HttpPut("/partners/screens/{screenId}/seat-layout")]
        [AuditAction("PARTNER_UPDATE_SEAT_LAYOUT", "SeatLayout", recordIdRouteKey: "screenId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_UPDATE")]
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
        [HttpPut("/partners/screens/{screenId}/seat-layout/{seatId}")]
        [AuditAction("PARTNER_UPDATE_SEAT_LAYOUT_ITEM", "SeatLayout", recordIdRouteKey: "seatId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_UPDATE")]
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
        /// Bulk create/update multiple seats
        /// </summary>
        [HttpPost("/partners/screens/{screenId}/seat-layout/bulk")]
        [AuditAction("PARTNER_CREATE_SEAT_LAYOUT_BULK", "SeatLayout", recordIdRouteKey: "screenId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_BULK_CREATE")]
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
                var errorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
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
        [HttpDelete("/partners/screens/{screenId}/seat-layout")]
        [AuditAction("PARTNER_DELETE_SEAT_LAYOUT", "SeatLayout", recordIdRouteKey: "screenId", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_DELETE")]
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
        [HttpDelete("/partners/screens/{screenId}/seat-layout/{seatId}")]
        [AuditAction("PARTNER_DELETE_SEAT_LAYOUT_ITEM", "SeatLayout", recordIdRouteKey: "seatId", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_DELETE")]
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
        [HttpDelete("/partners/screens/{screenId}/seat-layout/bulk")]
        [AuditAction("PARTNER_DELETE_SEAT_LAYOUT_BULK", "SeatLayout", recordIdRouteKey: "screenId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SEAT_LAYOUT_BULK_DELETE")]
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
        /// Get showtime by ID (View-only)
        /// </summary>
        [HttpGet("/partners/showtimes/{showtimeId}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SHOWTIME_READ")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeById(int showtimeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var result = await _showtimeService.GetPartnerShowtimeByIdAsync(partnerId, showtimeId, userId);

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
        [HttpGet("/partners/showtimes")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SHOWTIME_READ")]
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
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
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

                var result = await _showtimeService.GetPartnerShowtimesAsync(partnerId, userId, request);

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

        /// <summary>
        /// Create a new showtime
        /// </summary>
        [HttpPost("/partners/showtimes")]
        [AuditAction("PARTNER_CREATE_SHOWTIME", "Showtime", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SHOWTIME_CREATE")]
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
                var result = await _showtimeService.CreatePartnerShowtimeAsync(partnerId, request, auditAction: "PARTNER_CREATE_SHOWTIME");

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
        /// Update showtime
        /// </summary>
        [HttpPut("/partners/showtimes/{showtimeId}")]
        [AuditAction("PARTNER_UPDATE_SHOWTIME", "Showtime", recordIdRouteKey: "showtimeId", includeRequestBody: true)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SHOWTIME_UPDATE")]
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
                var result = await _showtimeService.UpdatePartnerShowtimeAsync(partnerId, showtimeId, request, auditAction: "PARTNER_UPDATE_SHOWTIME");

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
        /// Delete showtime (Soft Delete)
        /// </summary>
        [HttpDelete("/partners/showtimes/{showtimeId}")]
        [AuditAction("PARTNER_DELETE_SHOWTIME", "Showtime", recordIdRouteKey: "showtimeId", includeRequestBody: false)]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SHOWTIME_DELETE")]
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
                var result = await _showtimeService.DeletePartnerShowtimeAsync(partnerId, showtimeId, auditAction: "PARTNER_DELETE_SHOWTIME");

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

        /// <summary>Create a new combo</summary>
        [HttpPost("/partners/services")]
        [AuditAction("PARTNER_CREATE_COMBO", "Combo", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<ServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var result = await _comboService.CreateAsync(partnerId, userId, request);

                return Ok(new SuccessResponse<ServiceResponse>
                {
                    Message = "Tạo combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = first, Errors = ex.Errors });
            }
            catch (ConflictException ex)
            {
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return Conflict(new ValidationErrorResponse { Message = first, Errors = ex.Errors });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi tạo combo." });
            }
        }

        /// <summary>Get combo by id</summary>
        [HttpGet("/partners/services/{serviceId}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SERVICE_READ")]
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
        [HttpGet("/partners/services")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("SERVICE_READ")]
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

        /// <summary>Update combo</summary>
        [HttpPut("/partners/services/{serviceId}")]
        [AuditAction("PARTNER_UPDATE_COMBO", "Combo", recordIdRouteKey: "serviceId", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<ServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateService(int serviceId, [FromBody] UpdateServiceRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var result = await _comboService.UpdateAsync(partnerId, userId, serviceId, request);

                return Ok(new SuccessResponse<ServiceResponse>
                {
                    Message = "Cập nhật combo thành công",
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
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi cập nhật combo." });
            }
        }

        /// <summary>Delete combo (soft: set is_available = false)</summary>
        [HttpDelete("/partners/services/{serviceId}")]
        [AuditAction("PARTNER_DELETE_COMBO", "Combo", recordIdRouteKey: "serviceId", includeRequestBody: false)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<ServiceActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteService(int serviceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();
                var result = await _comboService.DeleteAsync(partnerId, userId, serviceId);

                return Ok(new SuccessResponse<ServiceActionResponse>
                {
                    Message = result.Message,
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
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi xóa combo." });
            }
        }

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
        [HttpGet("/partners/bookings")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("BOOKING_READ")]
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
        [HttpGet("/partners/bookings/{bookingId}")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("BOOKING_READ")]
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
        /// Get partner's booking statistics from their cinemas
        /// Partner can only view statistics from their own cinemas
        /// </summary>
        /// <param name="fromDate">Filter from booking date (ISO format). If null, defaults to 30 days ago</param>
        /// <param name="toDate">Filter to booking date (ISO format). If null, defaults to today</param>
        /// <param name="cinemaId">Filter by cinema ID (if null, get statistics for all partner's cinemas)</param>
        /// <param name="groupBy">Group by time period: "day", "week", "month", "year" (default: "day")</param>
        /// <param name="includeComparison">Include comparison with previous period (default: false)</param>
        /// <param name="topLimit">Limit for top items (default: 10)</param>
        /// <param name="page">Page number for paginated lists (default: 1)</param>
        /// <param name="pageSize">Page size for paginated lists (default: 20, max: 100)</param>
        /// <returns>Booking statistics</returns>
        [HttpGet("/partners/bookings/statistics")]
        [Authorize(Roles = "Partner,Staff")]
        [RequirePermission("BOOKING_STATISTICS")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerBookingStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerBookingStatistics(
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

                var result = await _partnerService.GetPartnerBookingStatisticsAsync(userId, request);

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
                    Message = "Lỗi khi lấy thống kê đơn hàng: " + ex.Message
                });
            }
        }

        // ==================== STATISTICS APIS ====================

        /// <summary>
        /// Get Staff Performance Statistics
        /// Shows which staff are performing best based on bookings from cinemas they manage
        /// </summary>
        [HttpGet("/partners/statistics/staff-performance")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<StaffPerformanceStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStaffPerformanceStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? cinemaId = null,
            [FromQuery] int topLimit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var request = new GetPartnerBookingStatisticsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    CinemaId = cinemaId,
                    TopLimit = topLimit
                };

                var result = await _partnerService.GetStaffPerformanceStatisticsAsync(userId, request);

                return Ok(new SuccessResponse<StaffPerformanceStatisticsResponse>
                {
                    Message = "Lấy thống kê hiệu suất nhân viên thành công.",
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
                    Message = "Lỗi khi lấy thống kê hiệu suất nhân viên: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Get Staff Performance Statistics for a specific staff
        /// </summary>
        [HttpGet("/partners/statistics/staff-performance/{employeeId}")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<StaffPerformanceStat>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStaffPerformanceDetail(
            [FromRoute] int employeeId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? cinemaId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var partnerId = await GetCurrentPartnerId();

                // Verify employee belongs to this partner
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.CinemaAssignments.Where(a => a.IsActive))
                        .ThenInclude(a => a.Cinema)
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId && e.RoleType == "Staff");

                if (employee == null)
                    throw new NotFoundException("Không tìm thấy nhân viên Staff hoặc không thuộc quyền quản lý của bạn.");

                var request = new GetPartnerBookingStatisticsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    CinemaId = cinemaId
                };

                // Get all staff stats and find the one we need
                var allStats = await _partnerService.GetStaffPerformanceStatisticsAsync(userId, request);
                var staffStat = allStats.StaffPerformance.FirstOrDefault(s => s.EmployeeId == employeeId);

                if (staffStat == null)
                {
                    // Staff has no bookings - return empty stats
                    var staffCinemaIds = employee.CinemaAssignments
                        .Where(a => a.IsActive)
                        .Select(a => a.CinemaId)
                        .ToList();

                    staffStat = new StaffPerformanceStat
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = employee.FullName,
                        Email = employee.User?.Email ?? "",
                        RoleType = employee.RoleType,
                        HireDate = employee.HireDate,
                        IsActive = employee.IsActive,
                        TotalBookings = 0,
                        TotalRevenue = 0,
                        AverageBookingValue = 0,
                        CinemaCount = staffCinemaIds.Count,
                        CinemaIds = staffCinemaIds,
                        CinemaNames = employee.CinemaAssignments
                            .Where(a => a.IsActive)
                            .Select(a => a.Cinema.CinemaName ?? "")
                            .Where(n => !string.IsNullOrEmpty(n))
                            .ToList(),
                        TotalTicketsSold = 0,
                        Rank = 0
                    };
                }

                return Ok(new SuccessResponse<StaffPerformanceStat>
                {
                    Message = "Lấy thống kê chi tiết nhân viên thành công.",
                    Result = staffStat
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
                    Message = "Lỗi khi lấy thống kê chi tiết nhân viên: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Get Cinema Cluster Statistics
        /// Groups cinemas by city/district and calculates statistics for each cluster
        /// </summary>
        [HttpGet("/partners/statistics/cinema-clusters")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaClusterStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCinemaClusterStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string groupBy = "city")
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _partnerService.GetCinemaClusterStatisticsAsync(userId, groupBy, fromDate, toDate);

                return Ok(new SuccessResponse<CinemaClusterStatisticsResponse>
                {
                    Message = "Lấy thống kê cụm rạp thành công.",
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
                    Message = "Lỗi khi lấy thống kê cụm rạp: " + ex.Message
                });
            }
        }

        // ==================== EMPLOYEE MANAGEMENT ====================

        /// <summary>
        /// Create a new employee (Staff, Marketing, or Cashier)
        /// </summary>
        [HttpPost("/partners/employees")]
        [AuditAction("PARTNER_CREATE_EMPLOYEE", "Employee", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<EmployeeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _employeeManagementService.CreateEmployeeAsync(partnerId, request);

                var response = new SuccessResponse<EmployeeResponse>
                {
                    Message = "Tạo nhân viên thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi tạo nhân viên."
                });
            }
        }

        /// <summary>
        /// Get all employees with pagination and filtering
        /// </summary>
        [HttpGet("/partners/employees")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedEmployeesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? roleType = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "fullName",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _employeeManagementService.GetEmployeesAsync(
                    partnerId, page, limit, roleType, isActive, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedEmployeesResponse>
                {
                    Message = "Lấy danh sách nhân viên thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách nhân viên."
                });
            }
        }

        /// <summary>
        /// Get employee by ID
        /// </summary>
        [HttpGet("/partners/employees/{employeeId}")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<EmployeeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById([FromRoute] int employeeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _employeeManagementService.GetEmployeeByIdAsync(partnerId, employeeId);

                var response = new SuccessResponse<EmployeeResponse>
                {
                    Message = "Lấy thông tin nhân viên thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin nhân viên."
                });
            }
        }

        /// <summary>
        /// Update employee
        /// </summary>
        [HttpPut("/partners/employees/{employeeId}")]
        [AuditAction("PARTNER_UPDATE_EMPLOYEE", "Employee", recordIdRouteKey: "employeeId", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<EmployeeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee([FromRoute] int employeeId, [FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                var result = await _employeeManagementService.UpdateEmployeeAsync(partnerId, employeeId, request);

                var response = new SuccessResponse<EmployeeResponse>
                {
                    Message = "Cập nhật nhân viên thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật nhân viên."
                });
            }
        }

        /// <summary>
        /// Delete employee (Soft Delete)
        /// </summary>
        [HttpDelete("/partners/employees/{employeeId}")]
        [AuditAction("PARTNER_DELETE_EMPLOYEE", "Employee", recordIdRouteKey: "employeeId", includeRequestBody: false)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee([FromRoute] int employeeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                await _employeeManagementService.DeleteEmployeeAsync(partnerId, employeeId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa nhân viên thành công",
                    Result = null
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
                    Message = "Đã xảy ra lỗi hệ thống khi xóa nhân viên."
                });
            }
        }

        // ==================== EMPLOYEE CINEMA ASSIGNMENT ====================

        /// <summary>
        /// Gán cinema cho Staff/Cashier (phân quyền quản lý cụm rạp). Mỗi rạp chỉ có thể có 1 Staff hoặc 1 Cashier.
        /// </summary>
        [HttpPost("/partners/employees/cinema-assignments")]
        [AuditAction("PARTNER_ASSIGN_EMPLOYEE_CINEMA", "EmployeeCinemaAssignment", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignCinemaToEmployee([FromBody] AssignCinemaToEmployeeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Request body không được để trống"
                    });
                }

                var partnerId = await GetCurrentPartnerId();
                var userId = GetCurrentUserId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                // Kiểm tra nếu là Cashier thì chỉ cho phép gán 1 rạp
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.PartnerId == partnerId);
                
                if (employee == null)
                {
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy nhân viên" });
                }

                if (employee.RoleType == "Cashier" && request.CinemaIds.Count > 1)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Thu ngân chỉ có thể được phân quyền cho một rạp. Vui lòng chỉ chọn một rạp."
                    });
                }

                // Loại bỏ duplicate cinemaIds để tránh gán trùng
                var uniqueCinemaIds = request.CinemaIds.Distinct().ToList();
                
                // Gán từng rạp cho nhân viên
                foreach (var cinemaId in uniqueCinemaIds)
                {
                    await _employeeCinemaAssignmentService.AssignCinemaToEmployeeAsync(
                        partnerId, request.EmployeeId, cinemaId, userId);
                }

                var response = new SuccessResponse<object>
                {
                    Message = $"Phân quyền quản lý {uniqueCinemaIds.Count} rạp cho nhân viên thành công",
                    Result = null
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
                    Message = "Đã xảy ra lỗi hệ thống khi phân quyền."
                });
            }
        }

        /// <summary>
        /// Hủy phân quyền cinema cho Staff/Cashier
        /// </summary>
        [HttpDelete("/partners/employees/cinema-assignments")]
        [AuditAction("PARTNER_REMOVE_EMPLOYEE_CINEMA", "EmployeeCinemaAssignment", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnassignCinemaFromEmployee([FromBody] UnassignCinemaFromEmployeeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Request body không được để trống"
                    });
                }

                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                // Loại bỏ duplicate cinemaIds để tránh hủy trùng
                var uniqueCinemaIds = request.CinemaIds.Distinct().ToList();

                // Hủy phân quyền từng rạp cho nhân viên
                foreach (var cinemaId in uniqueCinemaIds)
                {
                    await _employeeCinemaAssignmentService.UnassignCinemaFromEmployeeAsync(
                        partnerId, request.EmployeeId, cinemaId);
                }

                var response = new SuccessResponse<object>
                {
                    Message = $"Hủy phân quyền quản lý {uniqueCinemaIds.Count} rạp thành công",
                    Result = null
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
                    Message = "Đã xảy ra lỗi hệ thống khi hủy phân quyền."
                });
            }
        }

        /// <summary>
        /// Lấy danh sách cinema được phân quyền cho một Staff/Cashier
        /// </summary>
        [HttpGet("/partners/employees/{employeeId}/cinema-assignments")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<List<EmployeeCinemaAssignmentResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeCinemaAssignments([FromRoute] int employeeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);

                // Validate employee belongs to partner
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId);

                if (employee == null)
                {
                    throw new NotFoundException("Không tìm thấy nhân viên");
                }

                var assignments = await _context.EmployeeCinemaAssignments
                    .Include(a => a.Cinema)
                    .Where(a => a.EmployeeId == employeeId && a.IsActive)
                    .Select(a => new EmployeeCinemaAssignmentResponse
                    {
                        AssignmentId = a.AssignmentId,
                        EmployeeId = a.EmployeeId,
                        EmployeeName = a.Employee.FullName,
                        CinemaId = a.CinemaId,
                        CinemaName = a.Cinema.CinemaName ?? "",
                        CinemaCity = a.Cinema.City,
                        AssignedAt = a.AssignedAt,
                        IsActive = a.IsActive
                    })
                    .ToListAsync();

                var response = new SuccessResponse<List<EmployeeCinemaAssignmentResponse>>
                {
                    Message = "Lấy danh sách phân quyền thành công",
                    Result = assignments
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách phân quyền."
                });
            }
        }

        // ============================================
        // PERMISSION MANAGEMENT APIs
        // ============================================

        /// <summary>
        /// Grant permissions to employee
        /// </summary>
        [HttpPost("/partners/employees/{employeeId}/permissions")]
        [AuditAction("PARTNER_GRANT_PERMISSION", "EmployeeCinemaPermission", recordIdRouteKey: "employeeId", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PermissionActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GrantPermissions(int employeeId, [FromBody] GrantPermissionRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var result = await _permissionService.GrantPermissionsAsync(partnerId, employeeId, request);

                return Ok(new SuccessResponse<PermissionActionResponse>
                {
                    Message = result.Message,
                    Result = result
                });
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
                    Message = "Đã xảy ra lỗi hệ thống khi cấp quyền."
                });
            }
        }

        /// <summary>
        /// Revoke permissions from employee
        /// </summary>
        [HttpDelete("/partners/employees/{employeeId}/permissions")]
        [AuditAction("PARTNER_REVOKE_PERMISSION", "EmployeeCinemaPermission", recordIdRouteKey: "employeeId", includeRequestBody: true)]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PermissionActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokePermissions(int employeeId, [FromBody] RevokePermissionRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var result = await _permissionService.RevokePermissionsAsync(partnerId, employeeId, request);

                return Ok(new SuccessResponse<PermissionActionResponse>
                {
                    Message = result.Message,
                    Result = result
                });
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
                    Message = "Đã xảy ra lỗi hệ thống khi thu hồi quyền."
                });
            }
        }

        /// <summary>
        /// Get employee permissions
        /// </summary>
        [HttpGet("/partners/employees/{employeeId}/permissions")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<EmployeePermissionsListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeePermissions(int employeeId, [FromQuery] List<int>? cinemaIds = null)
        {
            try
            {
                var result = await _permissionService.GetEmployeePermissionsAsync(employeeId, cinemaIds);

                return Ok(new SuccessResponse<EmployeePermissionsListResponse>
                {
                    Message = "Lấy danh sách quyền thành công",
                    Result = result
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách quyền."
                });
            }
        }

        /// <summary>
        /// Get all available permissions in system
        /// </summary>
        [HttpGet("/partners/permissions")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<AvailablePermissionsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAvailablePermissions()
        {
            try
            {
                var result = await _permissionService.GetAvailablePermissionsAsync();

                return Ok(new SuccessResponse<AvailablePermissionsResponse>
                {
                    Message = "Lấy danh sách quyền có sẵn thành công",
                    Result = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách quyền."
                });
            }
        }

        /// <summary>
        /// Get Staff Profile - Lấy thông tin quyền và rạp được phân công của Staff
        /// </summary>
        [HttpGet("/partners/staff/profile")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<StaffProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStaffProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _staffService.GetStaffProfileAsync(userId);

                return Ok(new SuccessResponse<StaffProfileResponse>
                {
                    Message = "Lấy thông tin hồ sơ nhân viên thành công",
                    Result = result
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin hồ sơ nhân viên."
                });
            }
        }

        /// <summary>
        /// Get My Permissions - Staff tự xem quyền của mình
        /// </summary>
        [HttpGet("/partners/staff/permissions")]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(SuccessResponse<EmployeePermissionsListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyPermissions([FromQuery] List<int>? cinemaIds = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _staffService.GetMyPermissionsAsync(userId, cinemaIds);

                return Ok(new SuccessResponse<EmployeePermissionsListResponse>
                {
                    Message = "Lấy danh sách quyền thành công",
                    Result = result
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách quyền."
                });
            }
        }
    }
}