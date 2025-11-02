using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/partners/movie-submissions")]
    [Authorize(Roles = "Partner")]
    [Produces("application/json")]
    public class PartnerMovieManagementController : ControllerBase
    {
        private readonly PartnerMovieManagementService _submissionService;
        private readonly IContractValidationService _contractValidationService;

        public PartnerMovieManagementController(
            PartnerMovieManagementService submissionService,
            IContractValidationService contractValidationService)
        {
            _submissionService = submissionService;
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
            var partner = await _submissionService.GetPartnerByUserIdAsync(userId);
            return partner.PartnerId;
        }

        private async Task ValidatePartnerContractAsync()
        {
            var partnerId = await GetCurrentPartnerId();
            await _contractValidationService.ValidatePartnerHasActiveContractAsync(partnerId);
        }

        // ==================== AVAILABLE ACTORS APIs ====================

        /// <summary>
        /// Get all available actors with pagination and filtering
        /// </summary>
        [HttpGet("actors/available")]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedAvailableActorsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAvailableActors(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                await ValidatePartnerContractAsync();

                var result = await _submissionService.GetAvailableActorsAsync(
                    page, limit, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedAvailableActorsResponse>
                {
                    Message = "Lấy danh sách diễn viên có sẵn thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Có thể đến từ ValidatePartnerHasActiveContractAsync(...)
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên."
                });
            }
        }

        /// <summary>
        /// Get actor details by ID
        /// </summary>
        [HttpGet("actors/available/{id}")]
        [ProducesResponseType(typeof(SuccessResponse<AvailableActorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAvailableActorById(int id)
        {
            try
            {
                await ValidatePartnerContractAsync();

                var result = await _submissionService.GetAvailableActorByIdAsync(id);

                var response = new SuccessResponse<AvailableActorResponse>
                {
                    Message = "Lấy thông tin diễn viên thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Có thể đến từ ValidatePartnerHasActiveContractAsync(...)
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin diễn viên."
                });
            }
        }

        // ==================== SUBMISSION ACTORS APIs ====================

        /// <summary>
        /// Add actor to movie submission (can select existing actor or create new one)
        /// </summary>
        [HttpPost("{submissionId}/actors")]
        [ProducesResponseType(typeof(SuccessResponse<SubmissionActorResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddActorToSubmission(
            int submissionId,
            [FromBody] AddActorToSubmissionRequest request)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.AddActorToSubmissionAsync(submissionId, partnerId, request);

                var response = new SuccessResponse<SubmissionActorResponse>
                {
                    Message = "Thêm diễn viên vào submission thành công",
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
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict, new ValidationErrorResponse
                {
                    Message = first,
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi thêm diễn viên."
                });
            }
        }

        /// <summary>
        /// Get the list of actors of the movie submission
        /// </summary>
        [HttpGet("{submissionId}/actors")]
        [ProducesResponseType(typeof(SuccessResponse<SubmissionActorsListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSubmissionActors(int submissionId)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.GetSubmissionActorsAsync(submissionId, partnerId);

                var response = new SuccessResponse<SubmissionActorsListResponse>
                {
                    Message = "Lấy danh sách diễn viên thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Từ ValidateSubmissionAccessAsync(...) khi status != Draft
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách diễn viên."
                });
            }
        }

        /// <summary>
        /// Update actor in movie submission (role, information)
        /// </summary>
        [HttpPut("{submissionId}/actors/{submissionActorId}")]
        [ProducesResponseType(typeof(SuccessResponse<SubmissionActorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSubmissionActor(
            int submissionId,
            int submissionActorId,
            [FromBody] UpdateSubmissionActorRequest request)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.UpdateSubmissionActorAsync(
                    submissionId, submissionActorId, partnerId, request);

                var response = new SuccessResponse<SubmissionActorResponse>
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
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (ConflictException ex)
            {
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict, new ValidationErrorResponse
                {
                    Message = first,
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật diễn viên."
                });
            }
        }

        /// <summary>
        /// Remove actor from movie submission
        /// </summary>
        [HttpDelete("{submissionId}/actors/{submissionActorId}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveActorFromSubmission(int submissionId, int submissionActorId)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                await _submissionService.RemoveActorFromSubmissionAsync(submissionId, submissionActorId, partnerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa diễn viên khỏi submission thành công"
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Từ ValidateSubmissionAccessAsync(...) khi status != Draft
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa diễn viên."
                });
            }
        }

        // ==================== MOVIE SUBMISSION CRUD METHODS ====================

        /// <summary>
        /// Create a new movie submission (Draft)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SuccessResponse<MovieSubmissionResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMovieSubmission([FromBody] CreateMovieSubmissionRequest request)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.CreateMovieSubmissionAsync(partnerId, request);

                var response = new SuccessResponse<MovieSubmissionResponse>
                {
                    Message = "Tạo bản nháp phim thành công",
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
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict,
                    new ValidationErrorResponse { Message = first, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo bản nháp phim."
                });
            }
        }

        /// <summary>
        /// Get a list of partner movie submissions
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SuccessResponse<PaginatedMovieSubmissionsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieSubmissions(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.GetMovieSubmissionsAsync(
                    partnerId, page, limit, status, search, sortBy, sortOrder);

                var response = new SuccessResponse<PaginatedMovieSubmissionsResponse>
                {
                    Message = "Lấy danh sách bản nháp phim thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Có thể đến từ ValidatePartnerHasActiveContractAsync(...)
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách bản nháp phim."
                });
            }
        }

        /// <summary>
        /// Get movie submission details
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SuccessResponse<MovieSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieSubmissionById(int id)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.GetMovieSubmissionByIdAsync(id, partnerId);

                var response = new SuccessResponse<MovieSubmissionResponse>
                {
                    Message = "Lấy thông tin bản nháp phim thành công",
                    Result = result
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Có thể đến từ ValidatePartnerHasActiveContractAsync(...)
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin bản nháp phim."
                });
            }
        }

        /// <summary>
        /// Update movie submission (only when in Draft status)
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SuccessResponse<MovieSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMovieSubmission(
            int id,
            [FromBody] UpdateMovieSubmissionRequest request)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.UpdateMovieSubmissionAsync(id, partnerId, request);

                var response = new SuccessResponse<MovieSubmissionResponse>
                {
                    Message = "Cập nhật bản nháp phim thành công",
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
            // Không bắt ConflictException ở đây vì service UpdateMovieSubmissionAsync không ném 409
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi cập nhật bản nháp phim."
                });
            }
        }

        /// <summary>
        /// Submit movie submission (Draft → Pending)
        /// </summary>
        [HttpPost("{id}/submit")]
        [ProducesResponseType(typeof(SuccessResponse<SubmitMovieSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitMovieSubmission(int id)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                var result = await _submissionService.SubmitMovieSubmissionAsync(id, partnerId);

                var response = new SuccessResponse<SubmitMovieSubmissionResponse>
                {
                    Message = "Nộp phim thành công. Vui lòng chờ quản trị viên xét duyệt.",
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
            catch (ConflictException ex)
            {
                // Trùng pending submission
                var first = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict,
                    new ValidationErrorResponse { Message = first, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi nộp phim."
                });
            }
        }

        /// <summary>
        /// Delete movie submission (Soft Delete - only in Draft state)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMovieSubmission(int id)
        {
            try
            {
                await ValidatePartnerContractAsync();
                var partnerId = await GetCurrentPartnerId();

                await _submissionService.DeleteMovieSubmissionAsync(id, partnerId);

                var response = new SuccessResponse<object>
                {
                    Message = "Xóa bản nháp phim thành công"
                };
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                // Từ ValidateSubmissionAccessAsync(...) khi status != Draft
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
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
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi xóa bản nháp phim."
                });
            }
        }
    }
}
