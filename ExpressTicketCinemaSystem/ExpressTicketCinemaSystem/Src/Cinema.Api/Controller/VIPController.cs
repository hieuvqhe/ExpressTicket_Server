using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/user/vip")]
    [Authorize(Roles = "User")]
    [Produces("application/json")]
    public class VIPController : ControllerBase
    {
        private readonly IVIPService _vipService;
        private readonly CinemaDbCoreContext _db;

        public VIPController(IVIPService vipService, CinemaDbCoreContext db)
        {
            _vipService = vipService;
            _db = db;
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

        private async Task<int> GetCustomerIdAsync(int userId)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                // Tạo customer mới nếu chưa có
                customer = new Customer
                {
                    UserId = userId,
                    LoyaltyPoints = 0
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            return customer.CustomerId;
        }

        /// <summary>
        /// Lấy thông tin VIP status của user hiện tại
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(SuccessResponse<VIPStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVIPStatus()
        {
            try
            {
                var userId = GetCurrentUserId();
                var customerId = await GetCustomerIdAsync(userId);
                var status = await _vipService.GetVIPStatusAsync(customerId);

                return Ok(new SuccessResponse<VIPStatusResponse>
                {
                    Message = "Lấy thông tin VIP thành công",
                    Result = status
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = "Xác thực thất bại", Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử tích điểm
        /// </summary>
        [HttpGet("points/history")]
        [ProducesResponseType(typeof(SuccessResponse<PointHistoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPointHistory([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                var customerId = await GetCustomerIdAsync(userId);
                var history = await _vipService.GetPointHistoryAsync(customerId, page, limit);

                return Ok(new SuccessResponse<PointHistoryResponse>
                {
                    Message = "Lấy lịch sử điểm thành công",
                    Result = history
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = "Xác thực thất bại", Errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Nhận quà nâng cấp VIP
        /// </summary>
        [HttpPost("benefits/upgrade-bonus")]
        [ProducesResponseType(typeof(SuccessResponse<ClaimBenefitResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClaimUpgradeBonus()
        {
            try
            {
                var userId = GetCurrentUserId();
                var customerId = await GetCustomerIdAsync(userId);
                var result = await _vipService.ClaimUpgradeBonusAsync(customerId);

                return Ok(new SuccessResponse<ClaimBenefitResponse>
                {
                    Message = result.Message,
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = "Xác thực thất bại", Errors = ex.Errors });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Nhận quà sinh nhật
        /// </summary>
        [HttpPost("benefits/birthday-bonus")]
        [ProducesResponseType(typeof(SuccessResponse<ClaimBenefitResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClaimBirthdayBonus()
        {
            try
            {
                var userId = GetCurrentUserId();
                var customerId = await GetCustomerIdAsync(userId);
                var result = await _vipService.ClaimBirthdayBonusAsync(customerId);

                return Ok(new SuccessResponse<ClaimBenefitResponse>
                {
                    Message = result.Message,
                    Result = result
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = "Xác thực thất bại", Errors = ex.Errors });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}










