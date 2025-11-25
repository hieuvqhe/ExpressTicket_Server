using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/cinema")]
    [Produces("application/json")]
    public class CatalogController : ControllerBase
    {

        private readonly ICatalogQueryService _service;
        private readonly IScreenService _screenService;
        private readonly IShowtimeService _showtimeService;

        public CatalogController(ICatalogQueryService service, IScreenService screenService, IShowtimeService showtimeService)
        {
            _service = service;
            _screenService = screenService;
            _showtimeService = showtimeService;
        }

        /// <summary>
        /// Overview rạp → phòng → giờ chiếu của 1 phim theo ngày, có filter + phân trang rạp.
        /// Query: date=yyyy-MM-dd&city=&district=&brand=&cinemaId=&screenType=&formatType=&timeFrom=HH:mm&timeTo=HH:mm&page=1&limit=10&sortBy=time|cinema|brand&sortOrder=asc|desc
        /// </summary>
        [HttpGet("movies/{movieId:int}/showtimes/overview")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<MovieShowtimesOverviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieShowtimesOverview(
            int movieId,
            [FromQuery] GetMovieShowtimesOverviewQuery query,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetMovieShowtimesOverviewAsync(movieId, query, ct);
                return Ok(new SuccessResponse<MovieShowtimesOverviewResponse>
                {
                    Message = "Lấy danh sách suất chiếu thành công",
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
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy suất chiếu." });
            }
        }

        /// <summary>
        /// Sơ đồ ghế của 1 suất chiếu (AVAILABLE|LOCKED|SOLD|BLOCKED).
        /// </summary>
        [HttpGet("showtimes/{showtimeId:int}/seats")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<ShowtimeSeatMapResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeSeats(int showtimeId, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetShowtimeSeatMapAsync(showtimeId, ct);
                return Ok(new SuccessResponse<ShowtimeSeatMapResponse>
                {
                    Message = "Lấy sơ đồ ghế thành công",
                    Result = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy sơ đồ ghế." });
            }
        }

        // ===== SSE ENDPOINT ĐÃ ĐƯỢC THAY THẾ BẰNG SIGNALR =====
        // Endpoint SSE cũ đã được thay thế bằng SignalR Hub tại /hubs/showtime-seat
        // Client nên:
        // 1. Gọi GET /api/cinema/showtimes/{showtimeId}/seats để lấy snapshot ban đầu
        // 2. Kết nối SignalR Hub và join group "showtime_{showtimeId}" để nhận realtime updates
        // 3. Lắng nghe events: SeatLocked, SeatReleased, SeatSold
        //
        // [Obsolete("SSE endpoint đã được thay thế bằng SignalR. Sử dụng /hubs/showtime-seat thay thế.")]
        // [HttpGet("showtimes/{showtimeId:int}/seats/stream")]
        // [AllowAnonymous]
        // public async Task GetShowtimeSeatsStream(int showtimeId, CancellationToken ct) { ... }
        /// <summary>
        /// Lấy danh sách suất chiếu theo rạp (cho màn home)
        /// </summary>
        [HttpGet("showtimes/by-cinema")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<CinemaShowtimesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCinemaShowtimes(
            [FromQuery] GetCinemaShowtimesQuery query,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetCinemaShowtimesAsync(query, ct);
                return Ok(new SuccessResponse<CinemaShowtimesResponse>
                {
                    Message = "Lấy danh sách suất chiếu theo rạp thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách suất chiếu." });
            }
        }

        /// <summary>
        /// Get screen by ID for public (View-only, no authentication required)
        /// </summary>
        [HttpGet("screens/{screen_id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<ScreenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetScreenById([FromRoute(Name = "screen_id")] int screenId)
        {
            try
            {
                var result = await _screenService.GetScreenByIdPublicAsync(screenId);

                var response = new SuccessResponse<ScreenResponse>
                {
                    Message = "Lấy thông tin phòng thành công",
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
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin phòng."
                });
            }
        }

        /// <summary>
        /// Get showtime by ID for public (View-only, no authentication required)
        /// </summary>
        [HttpGet("showtimes/{showtimeId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<PartnerShowtimeDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeById(int showtimeId)
        {
            try
            {
                var result = await _showtimeService.GetShowtimeByIdPublicAsync(showtimeId);

                var response = new SuccessResponse<PartnerShowtimeDetailResponse>
                {
                    Message = "Lấy thông tin showtime thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin suất chiếu."
                });
            }
        }

        // ===== SSE Helper đã không còn cần thiết (đã chuyển sang SignalR) =====
    }
}
