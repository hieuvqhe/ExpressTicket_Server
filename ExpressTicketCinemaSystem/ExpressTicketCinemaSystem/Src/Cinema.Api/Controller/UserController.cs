using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")] 
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
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

        /// <summary>
        /// Get the current user's profile information
        /// </summary>
        [HttpGet("me")] 
        [ProducesResponseType(typeof(SuccessResponse<UserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var userId = GetCurrentUserId();
                var profile = await _userService.GetProfileAsync(userId);

                return Ok(new SuccessResponse<UserProfileResponse>
                {
                    Message = "Lấy hồ sơ thành công.",
                    Result = profile
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
        /// Update current user's profile information
        /// </summary>
        [HttpPatch("me")]
        [ProducesResponseType(typeof(SuccessResponse<UserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updated = await _userService.UpdateProfileAsync(userId, request);

                return Ok(new SuccessResponse<UserProfileResponse>
                {
                    Message = "Cập nhật hồ sơ thành công.",
                    Result = updated
                });
            }
            catch (ValidationException ex) 
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
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
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _userService.ChangePasswordAsync(userId, request);

                return Ok(new SuccessResponse<object>
                {
                    Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
                });
            }
            catch (ValidationException ex) 
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi khi đổi mật khẩu." });
            }
        }

        /// <summary>
        /// (Step 1) Request to change email, send code to OLD email
        /// </summary>
        [HttpPost("request-email-change")]
        [ProducesResponseType(typeof(SuccessResponse<RequestEmailChangeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestEmailChange()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _userService.RequestEmailChangeAsync(userId);

                return Ok(new SuccessResponse<RequestEmailChangeResponse>
                {
                    Message = "Mã xác thực đã được gửi tới email hiện tại.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Lỗi khi khởi tạo yêu cầu đổi email." });
            }
        }

        /// <summary>
        /// (Step 2) Verify the code sent to the OLD email
        /// </summary>
        [HttpPost("verify-email-change/current")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyEmailChangeCurrent([FromBody] VerifyEmailChangeRequest req)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _userService.VerifyCurrentEmailCodeAsync(userId, req.RequestId, req.Code);

                return Ok(new SuccessResponse<object> { Message = "Xác thực email hiện tại thành công." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Lỗi khi xác thực mã hiện tại." });
            }
        }

        /// <summary>
        /// (Step 3) Provide NEW email, send code to NEW email
        /// </summary>
        [HttpPost("submit-new-email")]
        [ProducesResponseType(typeof(SuccessResponse<RequestEmailChangeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitNewEmail([FromBody] SubmitNewEmailRequest req)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _userService.SubmitNewEmailAsync(userId, req);

                return Ok(new SuccessResponse<RequestEmailChangeResponse>
                {
                    Message = "Mã xác minh đã được gửi tới email mới.",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse { Message = "Xung đột dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Lỗi khi gửi mã tới email mới." });
            }
        }

        /// <summary>
        /// (Step 4) Verify the code sent to the NEW email
        /// </summary>
        [HttpPost("verify-email-change/new")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyEmailChangeNew([FromBody] VerifyEmailChangeRequest req)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _userService.VerifyNewEmailCodeAsync(userId, req.RequestId, req.Code);

                return Ok(new SuccessResponse<object> { Message = "Xác thực email mới thành công." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Lỗi khi xác thực mã email mới."});
            }
        }

        /// <summary>
        /// (Step 5) Complete the email change (after both have been verified)
        /// </summary>
        [HttpPost("complete-email-change")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteEmailChange([FromBody] CompleteEmailChangeRequest req)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _userService.CompleteEmailChangeAsync(userId, req.RequestId);

                return Ok(new SuccessResponse<object> { Message = "Thay đổi email thành công." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse { Message = "Lỗi xác thực dữ liệu", Errors = ex.Errors });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse { Message = "Xung đột dữ liệu", Errors = ex.Errors });
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
                return StatusCode(500, new ErrorResponse { Message = "Lỗi khi hoàn tất đổi email." });
            }
        }
    }
}