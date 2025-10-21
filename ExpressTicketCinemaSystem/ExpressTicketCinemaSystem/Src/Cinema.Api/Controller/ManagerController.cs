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

        public ManagerController(ContractService contractService , PartnerService partnerService )
        {
            _contractService = contractService;
            _partnerService = partnerService;
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

    }
}