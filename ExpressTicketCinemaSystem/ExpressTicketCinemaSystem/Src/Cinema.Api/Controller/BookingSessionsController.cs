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
    [Route("api/booking/sessions")]
    [Produces("application/json")]
    public class BookingSessionsController : ControllerBase
    {
        private readonly IBookingSessionService _service;

        public BookingSessionsController(IBookingSessionService service)
        {
            _service = service;
        }

        /// <summary>
        /// Tạo booking session (DRAFT) với TTL ~10 phút. Cho phép anonymous.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingSessionRequest request,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.CreateAsync(User, request, ct);

                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Tạo session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = msg,
                    Errors = ex.Errors
                });
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
                    Message = "Đã xảy ra lỗi hệ thống khi tạo session."
                });
            }
        }
        /// <summary>
        /// Lấy chi tiết session (items, pricing, state, TTL). Cho phép anonymous.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAsync(id, ct);
                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Lấy thông tin session thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin session."
                });
            }
        }

        /// <summary>
        /// Gia hạn TTL session (~10') và seat locks (~3'). Cho phép anonymous.
        /// </summary>
        [HttpPost("{id:guid}/touch")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Touch(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _service.TouchAsync(id, ct);
                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Gia hạn session thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi gia hạn session."
                });
            }
        }
    }
}
