// 3. ManagerContractsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests.ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/manager")]
    [Authorize(Roles = "Manager")]
    [Produces("application/json")]
    public class ManagerController : ControllerBase
    {
        private readonly ContractService _contractService;
        private readonly PartnerService _partnerService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IManagerService _managerService;

        public ManagerController(ContractService contractService , PartnerService partnerService, IAzureBlobService azureBlobService, IManagerService managerService)
        {
            _contractService = contractService;
            _partnerService = partnerService;
            _azureBlobService = azureBlobService;
            _managerService = managerService;
        }

        private int GetCurrentManagerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(idClaim, out var id))
            {
                return id;
            }

            throw new UnauthorizedException("Token không hợp lệ hoặc không chứa ID người dùng.");
        }

        /// <summary>
        /// Create a new contract draft for PDF generation
        /// </summary>
        [HttpPost("/manager/contracts")]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _contractService.CreateContractDraftAsync(managerId, request);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Tạo hợp đồng draft thành công. Dữ liệu đã sẵn sàng để tạo PDF.",
                    Result = result
                };
                return StatusCode(StatusCodes.Status201Created, response);
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
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo hợp đồng."
                });
            }
        }
        /// <summary>
        /// Get all contracts with filtering, sorting and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="managerId">Filter by manager ID</param>
        /// <param name="partnerId">Filter by partner ID</param>
        /// <param name="status">Filter by contract status (draft, pending, active, expired)</param>
        /// <param name="search">Search term for contract number, title, or partner name</param>
        /// <param name="sortBy">Field to sort by (contract_number, title, start_date, end_date, commission_rate, status, created_at, updated_at, partner_name)</param>
        /// <param name="sortOrder">Sort order (asc, desc)</param>
        /// <returns>Paginated list of contracts</returns>
        [HttpGet("/manager/contracts")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedContractsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllContracts(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] int? managerId = null,
            [FromQuery] int? partnerId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var currentManagerId = GetCurrentManagerId();

                // Nếu không có managerId filter, mặc định lấy contracts của manager hiện tại
                var effectiveManagerId = managerId ?? currentManagerId;

                var result = await _contractService.GetAllContractsAsync(
                    effectiveManagerId, page, limit, managerId, partnerId, status, search, sortBy, sortOrder);

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
        /// Get contract details by ID
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <returns>Contract details</returns>
        [HttpGet("/manager/contracts/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContractById(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _contractService.GetContractByIdAsync(id, managerId);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Lấy thông tin hợp đồng thành công",
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
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = "Xác thực thất bại",
                    Errors = ex.Errors
                });
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
        /// Send contract PDF to partner for signing
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <param name="request">PDF information</param>
        /// <returns>Success message</returns>
        [HttpPost("/manager/contracts/{id}/send-pdf")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendContractPdfToPartner(int id, [FromBody] SendContractPdfRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                await _contractService.SendContractPdfToPartnerAsync(id, managerId, request);

                var response = new SuccessResponse<object>
                {
                    Message = "Gửi hợp đồng PDF đến partner thành công. Partner đã nhận được email với link tải PDF."
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
                    Message = "Đã xảy ra lỗi hệ thống khi gửi hợp đồng PDF."
                });
            }
        }
        /// <summary>
        /// Finalize and lock contract after partner signing
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <param name="request">Finalize contract request</param>
        /// <returns>Finalized contract details</returns>
        [HttpPut("/manager/contracts/{id}/finalize")]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FinalizeContract(int id, [FromBody] FinalizeContractRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _contractService.FinalizeContractAsync(id, managerId, request);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Hợp đồng đã được khóa và hoàn tất thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi hoàn tất hợp đồng."
                });
            }
        }
        /// <summary>
        /// Get list of pending partners for approval
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="search">Search term for partner name, email, phone, or tax code</param>
        /// <param name="sortBy">Field to sort by (partner_name, email, phone, tax_code, created_at, updated_at)</param>
        /// <param name="sortOrder">Sort order (asc, desc)</param>
        /// <returns>Paginated list of pending partners</returns>
        [HttpGet("/manager/partners/pending")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedPartnersResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingPartners(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var result = await _partnerService.GetPendingPartnersAsync(page, limit, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedPartnersResponse>
                {
                    Message = "Lấy danh sách partner chờ duyệt thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách partner chờ duyệt."
                });
            }
        }
        /// <summary>
        /// Approve a pending partner
        /// </summary>
        /// <param name="id">Partner ID</param>
        /// <returns>Approved partner details</returns>
        [HttpPut("/manager/partners/{id}/approve")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerApprovalResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApprovePartner(int id) 
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _partnerService.ApprovePartnerAsync(id, managerId); 

                var response = new SuccessResponse<PartnerApprovalResponse>
                {
                    Message = "Duyệt partner thành công. Partner đã có thể đăng nhập vào hệ thống.",
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
                    Message = "Đã xảy ra lỗi hệ thống khi duyệt partner."
                });
            }
        }
        /// <summary>
        /// Reject a pending partner
        /// </summary>
        /// <param name="id">Partner ID</param>
        /// <param name="request">Reject request with reason</param>
        /// <returns>Rejected partner details</returns>
        [HttpPut("/manager/partners/{id}/reject")]
        [ProducesResponseType(typeof(SuccessResponse<PartnerRejectionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectPartner(int id, [FromBody] RejectPartnerRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _partnerService.RejectPartnerAsync(id, managerId, request);

                var response = new SuccessResponse<PartnerRejectionResponse>
                {
                    Message = "Từ chối partner thành công.",
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
                    Message = "Đã xảy ra lỗi hệ thống khi từ chối partner."
                });
            }
        }
        /// <summary>
        /// Get list of partners without any contracts
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="search">Search term for partner name</param>
        /// <returns>Paginated list of partners without contracts</returns>
        [HttpGet("/manager/partners/without-contracts")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedPartnersWithoutContractsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnersWithoutContracts(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _partnerService.GetPartnersWithoutContractsAsync(page, limit, search);

                var response = new SuccessResponse<PaginatedPartnersWithoutContractsResponse>
                {
                    Message = "Lấy danh sách partner chưa có hợp đồng thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách partner chưa có hợp đồng."
                });
            }
        }
        /// <summary>
        /// Update contract draft (only for draft status)
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <param name="request">Update contract request</param>
        /// <returns>Updated contract details</returns>
        [HttpPut("/manager/contracts/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateContract(int id, [FromBody] UpdateContractRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _contractService.UpdateContractDraftAsync(id, managerId, request);

                var response = new SuccessResponse<ContractResponse>
                {
                    Message = "Cập nhật hợp đồng draft thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật hợp đồng."
                });
            }
        }
        /// <summary>
        /// Cancel contract draft (soft delete with cancelled status)
        /// </summary>
        /// <param name="id">Contract ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("/manager/contracts/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelContract(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                await _contractService.CancelContractAsync(id, managerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Hủy hợp đồng thành công"
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
                    Message = "Đã xảy ra lỗi hệ thống khi hủy hợp đồng."
                });
            }
        }
        /// <summary>
        /// Generate SAS URL for PDF upload to Azure Blob Storage
        /// </summary>
        /// <param name="request">File name information</param>
        /// <returns>SAS URL for upload and permanent blob URL</returns>
        [HttpPost("/manager/contracts/generate-upload-sas")]
        [ProducesResponseType(typeof(SuccessResponse<GeneratePdfUploadUrlResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GeneratePdfUploadUrl([FromBody] GeneratePdfUploadUrlRequest request)
        {
            try
            {
                // ==================== VALIDATION SECTION ====================
                if (string.IsNullOrWhiteSpace(request.FileName))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Tên file là bắt buộc",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["fileName"] = new ValidationError
                            {
                                Msg = "Tên file không được để trống",
                                Path = "fileName",
                                Location = "body"
                            }
                        }
                    });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".pdf" };
                var extension = Path.GetExtension(request.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Định dạng file không hợp lệ",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["fileName"] = new ValidationError
                            {
                                Msg = "Chỉ chấp nhận file PDF",
                                Path = "fileName",
                                Location = "body"
                            }
                        }
                    });
                }

                // ==================== BUSINESS LOGIC SECTION ====================
                var result = await _azureBlobService.GeneratePdfUploadUrlAsync(request.FileName);

                var response = new SuccessResponse<GeneratePdfUploadUrlResponse>
                {
                    Message = "Tạo SAS URL thành công",
                    Result = result
                };

                return Ok(response);
            }
            catch (Exception ex)
            { 
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo URL upload PDF."
                });
            }
        }

        /// <summary>
        /// Get all booking orders (Manager only) with filtering and pagination
        /// </summary>
        /// <remarks>
        /// Manager only - View all bookings from all partners and all cinemas
        /// </remarks>
        /// <param name="partnerId">Filter by partner ID</param>
        /// <param name="cinemaId">Filter by cinema ID</param>
        /// <param name="status">Filter by booking status</param>
        /// <param name="paymentStatus">Filter by payment status</param>
        /// <param name="fromDate">Filter from booking date</param>
        /// <param name="toDate">Filter to booking date</param>
        /// <param name="customerId">Filter by customer ID</param>
        /// <param name="customerEmail">Search by customer email</param>
        /// <param name="customerPhone">Search by customer phone</param>
        /// <param name="customerName">Search by customer name</param>
        /// <param name="bookingCode">Search by booking code</param>
        /// <param name="orderCode">Search by order code</param>
        /// <param name="movieId">Filter by movie ID</param>
        /// <param name="minAmount">Filter by minimum amount</param>
        /// <param name="maxAmount">Filter by maximum amount</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="sortBy">Sort by field</param>
        /// <param name="sortOrder">Sort order (asc/desc)</param>
        [HttpGet("/manager/bookings")]
        [ProducesResponseType(typeof(SuccessResponse<ManagerBookingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBookings(
            [FromQuery] int? partnerId,
            [FromQuery] int? cinemaId,
            [FromQuery] string? status,
            [FromQuery] string? paymentStatus,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? customerId,
            [FromQuery] string? customerEmail,
            [FromQuery] string? customerPhone,
            [FromQuery] string? customerName,
            [FromQuery] string? bookingCode,
            [FromQuery] string? orderCode,
            [FromQuery] int? movieId,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "booking_time",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var userId = GetCurrentManagerId();

                var request = new GetManagerBookingsRequest
                {
                    PartnerId = partnerId,
                    CinemaId = cinemaId,
                    Status = status,
                    PaymentStatus = paymentStatus,
                    FromDate = fromDate,
                    ToDate = toDate,
                    CustomerId = customerId,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    CustomerName = customerName,
                    BookingCode = bookingCode,
                    OrderCode = orderCode,
                    MovieId = movieId,
                    MinAmount = minAmount,
                    MaxAmount = maxAmount,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var result = await _managerService.GetManagerBookingsAsync(userId, request);

                return Ok(new SuccessResponse<ManagerBookingsResponse>
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
                return StatusCode(403, new ValidationErrorResponse
                {
                    Message = ex.Message,
                    Errors = ex.Errors
                });
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
        /// Get detailed information of a specific booking by ID (Manager only)
        /// </summary>
        /// <param name="bookingId">The ID of the booking to retrieve</param>
        [HttpGet("/manager/bookings/{bookingId}")]
        [ProducesResponseType(typeof(SuccessResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBookingDetail(int bookingId)
        {
            try
            {
                var userId = GetCurrentManagerId();
                var result = await _managerService.GetBookingDetailAsync(userId, bookingId);

                return Ok(new SuccessResponse<BookingDetailResponse>
                {
                    Message = "Lấy chi tiết đơn hàng thành công.",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new ValidationErrorResponse
                {
                    Message = ex.Message,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message
                });
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
        /// Get booking statistics (Manager only) with filtering and time period
        /// </summary>
        /// <remarks>
        /// Manager only - View statistics from all partners and all cinemas
        /// Returns comprehensive statistics including revenue by cinema, top customers, partner statistics, movie statistics, time-based statistics, voucher usage, and payment statistics
        /// </remarks>
        /// <param name="fromDate">Filter from booking date (default: 30 days ago)</param>
        /// <param name="toDate">Filter to booking date (default: today)</param>
        /// <param name="partnerId">Filter by partner ID (null = all partners)</param>
        /// <param name="cinemaId">Filter by cinema ID (null = all cinemas)</param>
        /// <param name="movieId">Filter by movie ID (null = all movies)</param>
        /// <param name="topLimit">Number of top items to return (default: 10, max: 50)</param>
        /// <param name="groupBy">Group by time period for trends: day, week, month (default: day)</param>
        /// <param name="includeComparison">Include comparison with previous period (default: true)</param>
        /// <param name="page">Page number for paginated lists (default: 1)</param>
        /// <param name="pageSize">Page size for paginated lists (default: 20, max: 100)</param>
        [HttpGet("/manager/bookings/statistics")]
        [ProducesResponseType(typeof(SuccessResponse<ManagerBookingStatisticsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBookingStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? partnerId,
            [FromQuery] int? cinemaId,
            [FromQuery] int? movieId,
            [FromQuery] int topLimit = 10,
            [FromQuery] string groupBy = "day",
            [FromQuery] bool includeComparison = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentManagerId();

                var request = new GetManagerBookingStatisticsRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    PartnerId = partnerId,
                    CinemaId = cinemaId,
                    MovieId = movieId,
                    TopLimit = topLimit,
                    GroupBy = groupBy,
                    IncludeComparison = includeComparison,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _managerService.GetBookingStatisticsAsync(userId, request);

                return Ok(new SuccessResponse<ManagerBookingStatisticsResponse>
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
                return StatusCode(403, new ValidationErrorResponse
                {
                    Message = ex.Message,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Lỗi khi lấy thống kê đơn hàng: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Get customers with successful bookings (Manager only)
        /// Returns top customers by booking count and total spent, plus full paginated list
        /// </summary>
        /// <param name="topLimit">Top N customers to return for each sort type (default: 5, max: 50)</param>
        /// <param name="fromDate">Filter from booking date (optional)</param>
        /// <param name="toDate">Filter to booking date (optional)</param>
        /// <param name="partnerId">Filter by partner ID (optional)</param>
        /// <param name="cinemaId">Filter by cinema ID (optional)</param>
        /// <param name="customerEmail">Search by customer email (optional)</param>
        /// <param name="customerName">Search by customer name (optional)</param>
        /// <param name="page">Page number for full list pagination (default: 1)</param>
        /// <param name="pageSize">Page size for full list pagination (default: 20, max: 100)</param>
        /// <param name="sortOrder">Sort order for full list: "asc" or "desc" (default: "desc")</param>
        /// <param name="sortBy">Sort by for full list: "booking_count" or "total_spent" (default: "booking_count")</param>
        /// <param name="topByBookingCountSortOrder">Sort order for top customers by booking count: "asc" or "desc" (default: "desc")</param>
        /// <param name="topByTotalSpentSortOrder">Sort order for top customers by total spent: "asc" or "desc" (default: "desc")</param>
        [HttpGet("/manager/customers/successful-bookings")]
        [ProducesResponseType(typeof(SuccessResponse<SuccessfulBookingCustomersResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSuccessfulBookingCustomers(
            [FromQuery] int topLimit = 5,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? partnerId = null,
            [FromQuery] int? cinemaId = null,
            [FromQuery] string? customerEmail = null,
            [FromQuery] string? customerName = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortOrder = "desc",
            [FromQuery] string sortBy = "booking_count",
            [FromQuery] string topByBookingCountSortOrder = "desc",
            [FromQuery] string topByTotalSpentSortOrder = "desc")
        {
            try
            {
                var userId = GetCurrentManagerId();

                var request = new GetSuccessfulBookingCustomersRequest
                {
                    TopLimit = topLimit,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PartnerId = partnerId,
                    CinemaId = cinemaId,
                    CustomerEmail = customerEmail,
                    CustomerName = customerName,
                    Page = page,
                    PageSize = pageSize,
                    SortOrder = sortOrder,
                    SortBy = sortBy,
                    TopByBookingCountSortOrder = topByBookingCountSortOrder,
                    TopByTotalSpentSortOrder = topByTotalSpentSortOrder
                };

                var result = await _managerService.GetSuccessfulBookingCustomersAsync(userId, request);

                return Ok(new SuccessResponse<SuccessfulBookingCustomersResponse>
                {
                    Message = "Lấy danh sách khách hàng thành công.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Dữ liệu đầu vào không hợp lệ.",
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi khi lấy danh sách khách hàng."
                });
            }
        }

    }
}