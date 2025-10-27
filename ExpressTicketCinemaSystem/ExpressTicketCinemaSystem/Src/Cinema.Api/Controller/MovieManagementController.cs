using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System.Security.Claims;
using System;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests;
using System.Linq; // Cần thêm using này để dùng FirstOrDefault()

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/movie-management")]
    [Authorize(Roles = "Manager")]
    [Produces("application/json")]
    public class MovieManagementController : ControllerBase
    {
        private readonly MovieManagementService _movieManagementService;

        public MovieManagementController(MovieManagementService movieManagementService)
        {
            _movieManagementService = movieManagementService;
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
        /// Get all actors with pagination and filtering
        /// </summary>
        [HttpGet("actors")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedActorsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)] // Thêm response type
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActors(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.GetActorsAsync(managerId, page, limit, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedActorsResponse>
                {
                    Message = "Lấy danh sách diễn viên thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (UnauthorizedException ex)
            {
                // SỬA THEO CÁCH 1
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                // Giữ nguyên
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên."
                });
            }
        }

        /// <summary>
        /// Get actor by ID
        /// </summary>
        [HttpGet("actors/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<ActorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActorById(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.GetActorByIdAsync(id, managerId);

                var response = new SuccessResponse<ActorResponse>
                {
                    Message = "Lấy thông tin diễn viên thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                // Giữ nguyên
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                // SỬA THEO CÁCH 1
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                // Giữ nguyên
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin diễn viên."
                });
            }
        }

        /// <summary>
        /// Create new actor
        /// </summary>
        [HttpPost("actors")]
        [ProducesResponseType(typeof(SuccessResponse<ActorResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateActor([FromBody] CreateActorRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.CreateActorAsync(managerId, request);

                var response = new SuccessResponse<ActorResponse>
                {
                    Message = "Tạo diễn viên thành công",
                    Result = result
                };
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ValidationException ex)
            {
                // Giữ nguyên (đã đúng theo Cách 1)
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                // SỬA THEO CÁCH 1
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedException ex)
            {
                // SỬA THEO CÁCH 1
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                // Giữ nguyên
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo diễn viên."
                });
            }
        }

        /// <summary>
        /// Update actor information
        /// </summary>
        [HttpPut("actors/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<ActorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateActor(int id, [FromBody] UpdateActorRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.UpdateActorAsync(id, managerId, request);

                var response = new SuccessResponse<ActorResponse>
                {
                    Message = "Cập nhật diễn viên thành công",
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
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật diễn viên."
                });
            }
        }

        /// <summary>
        /// Delete actor
        /// </summary>
        [HttpDelete("actors/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteActor(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                await _movieManagementService.DeleteActorAsync(id, managerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa diễn viên thành công"
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa diễn viên."
                });
            }
        }

        /// <summary>
        /// Create new movie - Có thể chọn actor có sẵn hoặc tạo mới
        /// </summary>
        [HttpPost("movies")]
        [ProducesResponseType(typeof(SuccessResponse<MovieResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMovie([FromBody] CreateMovieRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.CreateMovieAsync(managerId, request);

                var response = new SuccessResponse<MovieResponse>
                {
                    Message = "Tạo phim thành công",
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
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo phim."
                });
            }
        }

        /// <summary>
        /// Update movie information - Có thể chọn actor có sẵn hoặc tạo mới
        /// </summary>
        [HttpPut("movies/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<MovieResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] UpdateMovieRequest request)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                var result = await _movieManagementService.UpdateMovieAsync(id, managerId, request);

                var response = new SuccessResponse<MovieResponse>
                {
                    Message = "Cập nhật phim thành công",
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
            catch (ConflictException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Dữ liệu bị xung đột";
                return Conflict(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật phim."
                });
            }
        }

        /// <summary>
        /// Delete movie
        /// </summary>
        [HttpDelete("movies/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                var managerId = GetCurrentManagerId();
                await _movieManagementService.DeleteMovieAsync(id, managerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa phim thành công"
                };
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa phim."
                });
            }
        }
    }
}