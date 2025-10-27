using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.TheaterManagement.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/cinema-management")]
    [Authorize(Roles = "Manager")]
    [Produces("application/json")]
    public class CinemaManagementController : ControllerBase
    {
        private readonly CinemaManagementService _cinemaManagementService;

        public CinemaManagementController(CinemaManagementService cinemaManagementService)
        {
            _cinemaManagementService = cinemaManagementService;
        }

        private int GetCurrentManagerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(idClaim, out var id))
                return id;

            throw new UnauthorizedException("Token không hợp lệ hoặc không chứa ID người dùng.");
        }

        /// <summary>
        /// Lấy danh sách rạp chiếu (có phân trang, tìm kiếm, sắp xếp)
        /// </summary>
        [HttpGet("theaters")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedCinemasResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCinemas(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "CinemaName",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _cinemaManagementService.GetCinemasAsync(managerId, page, limit, search, sortBy, sortOrder);

                return Ok(new SuccessResponse<PaginatedCinemasResponse>
                {
                    Message = "Lấy danh sách rạp chiếu thành công",
                    Result = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi khi lấy danh sách rạp." });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một rạp chiếu
        /// </summary>
        [HttpGet("theaters/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCinemaById(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _cinemaManagementService.GetCinemaByIdAsync(id, managerId);

                return Ok(new SuccessResponse<CinemaResponse>
                {
                    Message = "Lấy thông tin rạp chiếu thành công",
                    Result = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Lỗi hệ thống khi lấy rạp chiếu." });
            }
        }

        /// <summary>
        /// Tạo rạp chiếu mới
        /// </summary>
        [HttpPost("theaters")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateCinema([FromBody] CreateCinemaRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _cinemaManagementService.CreateCinemaAsync(managerId, request);

                return StatusCode(StatusCodes.Status201Created, new SuccessResponse<CinemaResponse>
                {
                    Message = "Tạo rạp chiếu thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse { Message = "Dữ liệu bị xung đột", Errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi tạo rạp chiếu." });
            }
        }

        /// <summary>
        /// Cập nhật thông tin rạp chiếu
        /// </summary>
        [HttpPut("theaters/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<CinemaResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateCinema(int id, [FromBody] UpdateCinemaRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _cinemaManagementService.UpdateCinemaAsync(id, managerId, request);

                return Ok(new SuccessResponse<CinemaResponse>
                {
                    Message = "Cập nhật rạp chiếu thành công",
                    Result = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi cập nhật rạp chiếu." });
            }
        }

        /// <summary>
        /// Xóa rạp chiếu
        /// </summary>
        [HttpDelete("theaters/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteCinema(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                await _cinemaManagementService.DeleteCinemaAsync(id, managerId);

                return Ok(new SuccessResponse<object>
                {
                    Message = "Xóa rạp chiếu thành công"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi xóa rạp chiếu." });
            }
        }
    }
}
