using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/partners/statistics")]
    [Authorize(Roles = "Partner")]
    [Produces("application/json")]
    public class PartnerStatisticsController : ControllerBase
    {
        private readonly IPartnerStatisticsService _partnerStatisticsService;
        private readonly CinemaDbCoreContext _context;
        private readonly IContractValidationService _contractValidationService;

        public PartnerStatisticsController(
            IPartnerStatisticsService partnerStatisticsService,
            CinemaDbCoreContext context,
            IContractValidationService contractValidationService)
        {
            _partnerStatisticsService = partnerStatisticsService;
            _context = context;
            _contractValidationService = contractValidationService;
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

        private async Task<int> GetCurrentPartnerId()
        {
            var userId = GetCurrentUserId();
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
            {
                throw new UnauthorizedException("Không tìm thấy thông tin Partner.");
            }

            return partner.PartnerId;
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
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                
                var result = await _partnerStatisticsService.GetCheckInStatsAsync(showtimeId, partnerId);

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
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                
                var result = await _partnerStatisticsService.GetChannelStatsAsync(showtimeId, partnerId);

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
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                
                var result = await _partnerStatisticsService.GetCustomerBehaviorAsync(showtimeId, partnerId);

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
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                
                var result = await _partnerStatisticsService.GetBookingDetailsAsync(bookingId, partnerId);

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

        /// <summary>
        /// Lấy thống kê check-in theo rạp (tất cả showtimes trong khoảng thời gian)
        /// </summary>
        [HttpGet("cinemas/{cinemaId}/checkin-stats")]
        [ProducesResponseType(typeof(SuccessResponse<List<CheckInStatsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCheckInStatsByCinema(
            [FromRoute] int cinemaId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var partnerId = await GetCurrentPartnerId();
                await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
                
                var result = await _partnerStatisticsService.GetCheckInStatsByCinemaAsync(cinemaId, partnerId, startDate, endDate);

                return Ok(new SuccessResponse<List<CheckInStatsResponse>>
                {
                    Message = "Lấy thống kê check-in theo rạp thành công",
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
    }
}























