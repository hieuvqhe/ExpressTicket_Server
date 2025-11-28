using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/cashier")]
    [Authorize(Roles = "Cashier")]
    [Produces("application/json")]
    public class CashierController : ControllerBase
    {
        private readonly ICashierService _cashierService;
        private readonly CinemaDbCoreContext _context;

        public CashierController(ICashierService cashierService, CinemaDbCoreContext context)
        {
            _cashierService = cashierService;
            _context = context;
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

        private async Task<(int employeeId, int cinemaId)> GetCashierInfoAsync()
        {
            var userId = GetCurrentUserId();
            
            var employee = await _context.Employees
                .Include(e => e.CinemaAssignments)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.RoleType == "Cashier" && e.IsActive);

            if (employee == null)
            {
                throw new UnauthorizedException("Không tìm thấy thu ngân hoặc thu ngân không hoạt động");
            }

            var assignment = employee.CinemaAssignments.FirstOrDefault(a => a.IsActive);
            if (assignment == null)
            {
                throw new UnauthorizedException("Thu ngân chưa được phân quyền rạp nào");
            }

            return (employee.EmployeeId, assignment.CinemaId);
        }

        /// <summary>
        /// Quét vé vào cửa
        /// </summary>
        [HttpPost("scan-ticket")]
        [ProducesResponseType(typeof(SuccessResponse<ScanTicketResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ScanTicket([FromBody] ScanTicketRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.QrCode))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "QR code không được để trống"
                    });
                }

                var (employeeId, cinemaId) = await GetCashierInfoAsync();
                var result = await _cashierService.ScanTicketAsync(request.QrCode, employeeId, cinemaId);

                if (!result.Success)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = result.Message
                    });
                }

                return Ok(new SuccessResponse<ScanTicketResponse>
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
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi quét vé."
                });
            }
        }

        /// <summary>
        /// Thống kê tỉ lệ check-in theo showtime
        /// </summary>
        [HttpGet("showtimes/{showtimeId}/checkin-stats")]
        [ProducesResponseType(typeof(SuccessResponse<CheckInStatsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCheckInStats([FromRoute] int showtimeId)
        {
            try
            {
                var (employeeId, cinemaId) = await GetCashierInfoAsync();
                var result = await _cashierService.GetCheckInStatsAsync(showtimeId, employeeId, cinemaId);

                return Ok(new SuccessResponse<CheckInStatsResponse>
                {
                    Message = "Lấy thống kê check-in thành công",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = ex.Message
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thống kê."
                });
            }
        }

        /// <summary>
        /// Thống kê theo kênh bán
        /// </summary>
        [HttpGet("showtimes/{showtimeId}/channel-stats")]
        [ProducesResponseType(typeof(SuccessResponse<ChannelStatsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetChannelStats([FromRoute] int showtimeId)
        {
            try
            {
                var (employeeId, cinemaId) = await GetCashierInfoAsync();
                var result = await _cashierService.GetChannelStatsAsync(showtimeId, employeeId, cinemaId);

                return Ok(new SuccessResponse<ChannelStatsResponse>
                {
                    Message = "Lấy thống kê kênh bán thành công",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = ex.Message
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thống kê."
                });
            }
        }

        /// <summary>
        /// Thống kê hành vi khách hàng
        /// </summary>
        [HttpGet("showtimes/{showtimeId}/customer-behavior")]
        [ProducesResponseType(typeof(SuccessResponse<CustomerBehaviorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerBehavior([FromRoute] int showtimeId)
        {
            try
            {
                var (employeeId, cinemaId) = await GetCashierInfoAsync();
                var result = await _cashierService.GetCustomerBehaviorAsync(showtimeId, employeeId, cinemaId);

                return Ok(new SuccessResponse<CustomerBehaviorResponse>
                {
                    Message = "Lấy thống kê hành vi khách hàng thành công",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = ex.Message
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thống kê."
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết booking cho hậu kiểm
        /// </summary>
        [HttpGet("bookings/{bookingId}/details")]
        [ProducesResponseType(typeof(SuccessResponse<BookingDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingDetails([FromRoute] int bookingId)
        {
            try
            {
                var (employeeId, cinemaId) = await GetCashierInfoAsync();
                var result = await _cashierService.GetBookingDetailsAsync(bookingId, employeeId, cinemaId);

                return Ok(new SuccessResponse<BookingDetailsResponse>
                {
                    Message = "Lấy chi tiết booking thành công",
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = ex.Message
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy chi tiết booking."
                });
            }
        }
    }
}

