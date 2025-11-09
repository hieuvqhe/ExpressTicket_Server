using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/booking/sessions/{id:guid}/seats")]
    [Produces("application/json")]
    public class BookingSessionsSeatsController : ControllerBase
    {
        private readonly ISeatLockAppService _service;

        public BookingSessionsSeatsController(ISeatLockAppService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lock nhiều ghế cho session (all-or-nothing). Nếu có ghế SOLD/LOCKED sẽ trả 409.
        /// Rule: tối đa 8 ghế, không để trống 1 ghế ở giữa.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<LockSeatsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Lock(Guid id, [FromBody] LockSeatsRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.LockAsync(id, request, ct);
                return Ok(new SuccessResponse<LockSeatsResponse>
                {
                    Message = "Lock ghế thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (ConflictException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict, new ValidationErrorResponse
                {
                    Message = msg,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lock ghế."
                });
            }
        }

        /// <summary>
        /// Release danh sách ghế do session này giữ.
        /// </summary>
        [HttpDelete]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<ReleaseSeatsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseSeatsRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.ReleaseAsync(id, request, ct);
                return Ok(new SuccessResponse<ReleaseSeatsResponse>
                {
                    Message = "Release ghế thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi release ghế."
                });
            }
        }
    }
}
