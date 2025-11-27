using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/manager/movie-submissions")]
    [Authorize(Roles = "Manager")]
    [Produces("application/json")]
    public class ManagerMovieSubmissionsController : ControllerBase
    {
        private readonly ManagerMovieSubmissionService _service;
        private readonly IManagerService _managerService;

        public ManagerMovieSubmissionsController(
            ManagerMovieSubmissionService service,
            IManagerService managerService)
        {
            _service = service;
            _managerService = managerService;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(idClaim, out var id)) return id;

            // 401
            throw new UnauthorizedException("Token không hợp lệ hoặc không chứa ID người dùng.");
        }

        private async Task<int> GetCurrentManagerIdAsync()
        {
            var userId = GetCurrentUserId();
            // Map userId -> managerId (401 nếu không phải manager)
            return await _managerService.GetManagerIdByUserIdAsync(userId);
        }

        // ------------------- GET ALL (non-draft) -------------------
        /// <summary>Lấy tất cả submissions KHÔNG phải Draft (Pending/Rejected/Resubmitted/Approved)</summary>
        [HttpGet]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                // xác thực manager (nếu token hỏng sẽ ném 401)
                _ = await GetCurrentManagerIdAsync();

                var result = await _service.GetAllNonDraftSubmissionsAsync(page, limit, status, search, sortBy, sortOrder);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Lấy danh sách submissions (non-draft) thành công",
                    Result = result
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách submissions."
                });
            }
        }

        // ------------------- GET BY ID (non-draft) -------------------
        /// <summary>Lấy chi tiết submission KHÔNG phải Draft</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _ = await GetCurrentManagerIdAsync();

                var result = await _service.GetNonDraftSubmissionByIdAsync(id);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Lấy thông tin submission thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy chi tiết submission."
                });
            }
        }

        // ------------------- GET PENDING -------------------
        /// <summary>Lấy danh sách submissions Pending</summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPending(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "submittedAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                _ = await GetCurrentManagerIdAsync();

                var result = await _service.GetPendingSubmissionsAsync(page, limit, search, sortBy, sortOrder);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Lấy danh sách submissions Pending thành công",
                    Result = result
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy submissions Pending."
                });
            }
        }

        // ------------------- APPROVE -------------------
        /// <summary>Duyệt submission (Pending → Approved). Tự động reject các Pending khác trùng tiêu đề.</summary>
        [HttpPut("{id:int}/approve")]
        [AuditAction("MANAGER_APPROVE_MOVIE_SUBMISSION", "MovieSubmission", recordIdRouteKey: "id", includeRequestBody: false)]
        [ProducesResponseType(typeof(SuccessResponse<MovieSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var managerId = await GetCurrentManagerIdAsync();

                var result = await _service.ApproveSubmissionAsync(id, managerId);
                return Ok(new SuccessResponse<MovieSubmissionResponse>
                {
                    Message = "Duyệt submission thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi duyệt submission."
                });
            }
        }

        // ------------------- REJECT -------------------
        public class RejectSubmissionRequest
        {
            public string? Reason { get; set; }
        }

        /// <summary>Từ chối submission (Pending → Rejected) với lý do.</summary>
        [HttpPut("{id:int}/reject")]
        [AuditAction("MANAGER_REJECT_MOVIE_SUBMISSION", "MovieSubmission", recordIdRouteKey: "id", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<MovieSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectSubmissionRequest request)
        {
            try
            {
                var managerId = await GetCurrentManagerIdAsync();

                var reason = request?.Reason ?? string.Empty;
                var result = await _service.RejectSubmissionAsync(id, managerId, reason);

                return Ok(new SuccessResponse<MovieSubmissionResponse>
                {
                    Message = "Từ chối submission thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi từ chối submission."
                });
            }
        }
    }
}
