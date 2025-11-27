using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/manager/vouchers")]
    [Authorize(Roles = "Manager")]
    [Produces("application/json")]
    public class ManagerVoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;
        private readonly CinemaDbCoreContext _context;

        public ManagerVoucherController(IVoucherService voucherService, CinemaDbCoreContext context)
        {
            _voucherService = voucherService;
            _context = context;
        }

        private async Task<int> GetCurrentManagerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                // Tìm ManagerId từ UserId
                var manager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                if (manager != null)
                {
                    return manager.ManagerId;
                }

                throw new UnauthorizedException("Người dùng không phải là manager.");
            }

            throw new UnauthorizedException("Token không hợp lệ hoặc không chứa ID người dùng.");
        }

        /// <summary>
        /// Create a new voucher
        /// </summary>
        [HttpPost]
        [AuditAction("MANAGER_CREATE_VOUCHER", "Voucher", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<VoucherResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.CreateVoucherAsync(managerId, request);

                var response = new SuccessResponse<VoucherResponse>
                {
                    Message = "Tạo voucher thành công",
                    Result = result
                };
                return StatusCode(StatusCodes.Status201Created, response);
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
                    Message = "Đã xảy ra lỗi hệ thống khi tạo voucher"
                });
            }
        }

        /// <summary>
        /// Get all vouchers with filtering, sorting and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="limit">Number of items per page (default: 10)</param>
        /// <param name="status">Filter by contract status (active, inactive)</param>
        /// <param name="search">Search term for voucherCode</param>
        /// <param name="sortBy">Field to sort by createdat, voucherId, voucherCode. discountVal ,validFrom, validTo, usageLimit, usedCount</param>
        /// <param name="sortOrder">Sort order (asc, desc)</param>
        /// <returns>Paginated list of contracts</returns>
        [HttpGet]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedVouchersResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllVouchers(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = "createdat",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.GetAllVouchersAsync(managerId, page, limit, search, status, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedVouchersResponse>
                {
                    Message = "Lấy danh sách voucher thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách voucher"
                });
            }
        }

        /// <summary>
        /// Get voucher details by ID
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SuccessResponse<VoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVoucherById(int id)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.GetVoucherByIdAsync(id, managerId);

                var response = new SuccessResponse<VoucherResponse>
                {
                    Message = "Lấy thông tin voucher thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin voucher"
                });
            }
        }

        /// <summary>
        /// Update voucher
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPut("{id}")]
        [AuditAction("MANAGER_UPDATE_VOUCHER", "Voucher", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<VoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] UpdateVoucherRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.UpdateVoucherAsync(id, managerId, request);

                var response = new SuccessResponse<VoucherResponse>
                {
                    Message = "Cập nhật voucher thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật voucher"
                });
            }
        }

        /// <summary>
        /// Soft delete voucher
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpDelete("{id}")]
        [AuditAction("MANAGER_DELETE_VOUCHER", "Voucher", recordIdRouteKey: "id", includeRequestBody: false)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                await _voucherService.SoftDeleteVoucherAsync(id, managerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa voucher thành công"
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
                    Message = "Đã xảy ra lỗi hệ thống khi xóa voucher"
                });
            }
        }

        /// <summary>
        /// Change status voucher
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPatch("{id}/toggle-status")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ToggleVoucherStatus(int id)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                await _voucherService.ToggleVoucherStatusAsync(id, managerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Thay đổi trạng thái voucher thành công"
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
                    Message = "Đã xảy ra lỗi hệ thống khi thay đổi trạng thái voucher"
                });
            }
        }

        /// <summary>
        /// Get all email voucher history
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpGet("{id}/email-history")]
        [ProducesResponseType(typeof(SuccessResponse<List<VoucherEmailHistoryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVoucherEmailHistory(int id)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.GetVoucherEmailHistoryAsync(id, managerId);

                var response = new SuccessResponse<List<VoucherEmailHistoryResponse>>
                {
                    Message = "Lấy lịch sử email thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy lịch sử email"
                });
            }
        }

        /// <summary>
        /// Send all user voucher
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPost("{id}/send-to-all-users")]
        [AuditAction("MANAGER_VOUCHER_SEND_ALL", "Voucher", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<SendVoucherEmailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVoucherToAllUsers(int id, [FromBody] SendVoucherToAllRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.SendVoucherToAllUsersAsync(id, managerId, request);

                var response = new SuccessResponse<SendVoucherEmailResponse>
                {
                    Message = $"Đã gửi voucher thành công đến {result.TotalSent} users, thất bại: {result.TotalFailed}",
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
                    Message = "Đã xảy ra lỗi hệ thống khi gửi voucher"
                });
            }
        }

        /// <summary>
        /// Sent voucher to specific user
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPost("{id}/send-to-specific-users")]
        [AuditAction("MANAGER_VOUCHER_SEND_SPECIFIC", "Voucher", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<SendVoucherEmailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVoucherToSpecificUsers(int id, [FromBody] SendVoucherEmailRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.SendVoucherToSpecificUsersAsync(id, managerId, request);

                var response = new SuccessResponse<SendVoucherEmailResponse>
                {
                    Message = $"Đã gửi voucher thành công đến {result.TotalSent} users mới, thất bại: {result.TotalFailed}",
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
                    Message = "Đã xảy ra lỗi hệ thống khi gửi voucher"
                });
            }
        }

        /// <summary>
        /// Send voucher to top buyers (customers with most bookings)
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPost("{id}/send-to-top-buyers")]
        [AuditAction("MANAGER_VOUCHER_SEND_TOP_BUYERS", "Voucher", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<SendVoucherEmailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVoucherToTopBuyers(int id, [FromBody] SendVoucherToTopBuyersRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.SendVoucherToTopBuyersAsync(id, managerId, request);

                var response = new SuccessResponse<SendVoucherEmailResponse>
                {
                    Message = $"Đã gửi voucher thành công đến {result.TotalSent} khách hàng top mua nhiều nhất, thất bại: {result.TotalFailed}",
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
                    Message = "Đã xảy ra lỗi hệ thống khi gửi voucher"
                });
            }
        }

        /// <summary>
        /// Send voucher to top spenders (customers with highest total spending)
        /// </summary>
        /// <param name="id">Voucher ID</param>
        [HttpPost("{id}/send-to-top-spenders")]
        [AuditAction("MANAGER_VOUCHER_SEND_TOP_SPENDERS", "Voucher", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<SendVoucherEmailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVoucherToTopSpenders(int id, [FromBody] SendVoucherToTopSpendersRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerId();
                var result = await _voucherService.SendVoucherToTopSpendersAsync(id, managerId, request);

                var response = new SuccessResponse<SendVoucherEmailResponse>
                {
                    Message = $"Đã gửi voucher thành công đến {result.TotalSent} khách hàng top chi tiêu nhiều nhất, thất bại: {result.TotalFailed}",
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
                    Message = "Đã xảy ra lỗi hệ thống khi gửi voucher"
                });
            }
        }
    }
}