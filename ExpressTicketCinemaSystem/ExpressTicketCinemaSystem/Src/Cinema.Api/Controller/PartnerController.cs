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

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/partners")]
    [Produces("application/json")]
    public class PartnersController : ControllerBase
    {
        private readonly PartnerService _partnerService;
        private readonly ContractService _contractService;
        private readonly IAzureBlobService _azureBlobService;
        public PartnersController(PartnerService partnerService , ContractService contractService , IAzureBlobService azureBlobService)
        {
            _partnerService = partnerService;
            _contractService = contractService;
            _azureBlobService = azureBlobService;
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
    }
}