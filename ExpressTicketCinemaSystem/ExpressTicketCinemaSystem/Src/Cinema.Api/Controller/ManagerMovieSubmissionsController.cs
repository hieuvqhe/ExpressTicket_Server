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
    [Authorize(Roles = "Manager,ManagerStaff")]
    [Produces("application/json")]
    public class ManagerMovieSubmissionsController : ControllerBase
    {
        private readonly ManagerMovieSubmissionService _service;
        private readonly IManagerService _managerService;
        private readonly IManagerStaffPermissionService _managerStaffPermissionService;

        public ManagerMovieSubmissionsController(
            ManagerMovieSubmissionService service,
            IManagerService managerService,
            IManagerStaffPermissionService managerStaffPermissionService)
        {
            _service = service;
            _managerService = managerService;
            _managerStaffPermissionService = managerStaffPermissionService;
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
        /// <summary>
        /// Lấy tất cả submissions KHÔNG phải Draft (Pending/Rejected/Resubmitted/Approved)
        /// Manager: Can view all submissions
        /// ManagerStaff: Can only view submissions from partners they have MOVIE_SUBMISSION_READ permission
        /// </summary>
        [HttpGet]
        [RequireManagerStaffPermission("MOVIE_SUBMISSION_READ")]
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
                var userId = GetCurrentUserId();
                var managerId = await GetCurrentManagerIdAsync();
                var managerStaffId = await _managerService.GetManagerStaffIdByUserIdAsync(userId);

                var result = await _service.GetAllNonDraftSubmissionsAsync(page, limit, status, search, sortBy, sortOrder, managerStaffId);
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
        /// <summary>
        /// Lấy chi tiết submission KHÔNG phải Draft
        /// Manager: Can view any submission
        /// ManagerStaff: Can only view submissions from partners they have MOVIE_SUBMISSION_READ permission
        /// </summary>
        [HttpGet("{id:int}")]
        [RequireManagerStaffPermission("MOVIE_SUBMISSION_READ")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var managerId = await GetCurrentManagerIdAsync();
                var managerStaffId = await _managerService.GetManagerStaffIdByUserIdAsync(userId);

                var result = await _service.GetNonDraftSubmissionByIdAsync(id, managerStaffId);
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
        /// <summary>
        /// Lấy danh sách submissions Pending
        /// Manager: Can view all pending submissions
        /// ManagerStaff: Can only view pending submissions from partners they have MOVIE_SUBMISSION_READ permission
        /// </summary>
        [HttpGet("pending")]
        [RequireManagerStaffPermission("MOVIE_SUBMISSION_READ")]
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
                var userId = GetCurrentUserId();
                var managerId = await GetCurrentManagerIdAsync();
                var managerStaffId = await _managerService.GetManagerStaffIdByUserIdAsync(userId);

                var result = await _service.GetPendingSubmissionsAsync(page, limit, search, sortBy, sortOrder, managerStaffId);
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
        /// <summary>
        /// Duyệt submission (Pending → Approved). Tự động reject các Pending khác trùng tiêu đề.
        /// Manager: Can approve any submission
        /// ManagerStaff: Can only approve submissions from partners they have MOVIE_SUBMISSION_APPROVE permission
        /// </summary>
        [HttpPut("{id:int}/approve")]
        [RequireManagerStaffPermission("MOVIE_SUBMISSION_APPROVE")]
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
                var userId = GetCurrentUserId();
                var managerId = await GetCurrentManagerIdAsync();
                var managerStaffId = await _managerService.GetManagerStaffIdByUserIdAsync(userId);

                var result = await _service.ApproveSubmissionAsync(id, managerId, managerStaffId);
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

        /// <summary>
        /// Từ chối submission (Pending → Rejected) với lý do.
        /// Manager: Can reject any submission
        /// ManagerStaff: Can only reject submissions from partners they have MOVIE_SUBMISSION_REJECT permission
        /// </summary>
        [HttpPut("{id:int}/reject")]
        [RequireManagerStaffPermission("MOVIE_SUBMISSION_REJECT")]
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
                var userId = GetCurrentUserId();
                var managerId = await GetCurrentManagerIdAsync();
                var managerStaffId = await _managerService.GetManagerStaffIdByUserIdAsync(userId);

                var reason = request?.Reason ?? string.Empty;
                var result = await _service.RejectSubmissionAsync(id, managerId, reason, managerStaffId);

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
