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

        public PartnersController(PartnerService partnerService, ContractService contractService, IAzureBlobService azureBlobService, IScreenService screenService, ISeatTypeService seatTypeService , ISeatLayoutService seatLayoutService , CinemaDbCoreContext context, IContractValidationService contractValidationService,  ICinemaService cinemaService, IShowtimeService showtimeService)
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

            // Tìm partner từ userId
            var partner = await _contractService.GetPartnerByUserId(userId);
            return partner.PartnerId;
        }

        /// <summary>
        /// Register a new partner
        /// </summary>
        [HttpPost("/partners/register")]
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
        /// Upload signature image for contract
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <param name="request">Signature upload request</param>
        /// <returns>Updated contract details</returns>
        [HttpPost("/partners/contracts/{id}/upload-signature")]
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
                    Message = "Upload ảnh biên bản ký thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi upload ảnh ký."
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
        /// (Partner) Tạo SAS URL để upload file ảnh chữ ký (PNG/JPG)
        /// </summary>
        /// <param name="request">Chứa tên file, ví dụ: "signature.png"</param>
        /// <returns>SAS URL (for uploading) and Blob URL (for saving)</returns>
        [HttpPost("/partners/contracts/generate-signature-upload-sas")]
        [Authorize(Roles = "Partner")] // Đảm bảo chỉ partner mới được gọi
        [ProducesResponseType(typeof(SuccessResponse<GeneratePdfUploadUrlResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
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

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(request.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Định dạng file không hợp lệ",
                    Errors = new Dictionary<string, ValidationError>
                    {
                        ["fileName"] = new ValidationError { Msg = "Chỉ chấp nhận file ảnh (.png, .jpg, .jpeg)" }
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo URL upload chữ ký."
                });
            }
        }
        /// <summary>
        /// Create a new cinema
        /// </summary>
        [HttpPost("/partners/cinemas")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
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
                    Message = "Đã xảy ra lỗi hệ thống khi tạo rạp."
                });
            }
        }
        /// <summary>
        /// Get cinema by ID
        /// </summary>
        [HttpGet("/partners/cinemas/{cinema_id}")]
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        /// Update cinema
        /// </summary>
        [HttpPut("/partners/cinemas/{cinema_id}")]
        [Authorize(Roles = "Partner")]
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
        [HttpDelete("/partners/cinemas/{cinema_id}")]
        [Authorize(Roles = "Partner")]
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
        [HttpPost("/partners/cinema/{cinema_id}/screens")]
        [Authorize(Roles = "Partner")]
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
        [HttpGet("/partners/screens/{screen_id}")]
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        /// Update screen for partner
        /// </summary>
        [HttpPut("/partners/screens/{screen_id}")]
        [Authorize(Roles = "Partner")]
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
        [HttpDelete("/partners/screens/{screen_id}")]
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [HttpPost("/partners/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Partner")]
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
        [HttpPut("/partners/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [HttpPost("/partners/screens/{screenId}/seat-layout/bulk")]
        [Authorize(Roles = "Partner")]
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
        [HttpDelete("/partners/screens/{screenId}/seat-layout")]
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [Authorize(Roles = "Partner")]
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
        [HttpPost("/partners/showtimes")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateShowtime([FromBody] PartnerShowtimeCreateRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
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
                    Message = "Xung đột dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo suất chiếu.",
                });
            }
        }

        
        /// <summary>
        /// Update showtime for partner
        /// </summary>
        [HttpPut("/partners/showtimes/{showtimeId}")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateShowtime(int showtimeId, [FromBody] PartnerShowtimeCreateRequest request)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
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
                    Message = "Xung đột dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật suất chiếu.",
                });
            }
        }

        // Thêm vào PartnersController.cs
        /// <summary>
        /// Soft delete showtime for partners
        /// </summary>
        [HttpDelete("/partners/showtimes/{showtimeId}")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeCreateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteShowtime(int showtimeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
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
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Xung đột dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa suất chiếu.",
                });
            }
        }

        // Thêm vào PartnersController.cs
        /// <summary>
        /// Get showtime by ID
        /// </summary>
        [HttpGet("/partners/showtimes/{showtimeId}")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeById(int showtimeId)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                var result = await _showtimeService.GetPartnerShowtimeByIdAsync(partnerId, showtimeId);

                var response = new SuccessResponse<PartnerShowtimeDetailResponse>
                {
                    Message = "Get showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin suất chiếu.",
                });
            }
        }

        /// <summary>
        /// Get all showtimes for partners with pagination and filtering.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="movie_id">Filter by movie ID</param>
        /// <param name="cinema_id">Filter by cinema ID</param>
        /// <param name="screen_id">Filter by screen ID</param>
        /// <param name="date">Filter by date (format: yyyy-MM-dd)</param>
        /// <param name="status">
        /// Filter by showtime status.
        /// Available values: scheduled, finished
        /// </param>
        /// <param name="sort_by">Field to sort by (default: start_time)</param>
        /// <param name="sort_order">Sort order. Available values: asc, desc (default: asc)</param>
        /// <returns>Paginated list of showtimes</returns>
        [HttpGet("/partners/showtimes")]
        [Authorize(Roles = "Partner")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
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

                var result = await _showtimeService.GetPartnerShowtimesAsync(partnerId, request);

                var response = new SuccessResponse<PartnerShowtimeListResponse>
                {
                    Message = "Get showtime thành công",
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách suất chiếu.",
                });
            }
        }

    }
}