using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Requests;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("user_id")?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }

        // View profile: POST /api/User/me
        [HttpPost("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _userService.GetProfileAsync(userId.Value);
            if (profile == null) return NotFound();

            return Ok(profile);
        }

        // Update profile: PATCH /api/User/me
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var updated = await _userService.UpdateProfileAsync(userId.Value, request);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                // validation errors
                return BadRequest(new { message = "Validation failed", detail = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi" });
            }
        }
        // POST /api/User/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Không xác định được user." });

            try
            {
                await _userService.ChangePasswordAsync(userId.Value, request);
                return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đổi mật khẩu." });
            }
        }
        [HttpPost("request-email-change")]
        public async Task<IActionResult> RequestEmailChange()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _userService.RequestEmailChangeAsync(userId.Value);
                return Ok(new { message = "Mã xác thực đã được gửi tới email hiện tại.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi khởi tạo yêu cầu đổi email.", detail = ex.Message });
            }
        }
        // Step 2: verify code sent to CURRENT email
        [HttpPost("verify-email-change/current")]
        public async Task<IActionResult> VerifyEmailChangeCurrent([FromBody] VerifyEmailChangeRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                await _userService.VerifyCurrentEmailCodeAsync(userId.Value, req.RequestId, req.Code);
                return Ok(new { message = "Xác thực email hiện tại thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xác thực mã hiện tại.", detail = ex.Message });
            }
        }
        // Step 3: submit new email
        [HttpPost("submit-new-email")]
        public async Task<IActionResult> SubmitNewEmail([FromBody] SubmitNewEmailRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // This will save newEmail into the request and send a code to newEmail
                var result = await _userService.SubmitNewEmailAsync(userId.Value, req);
                return Ok(new { message = "Mã xác minh đã được gửi tới email mới.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gửi mã tới email mới.", detail = ex.Message });
            }
        }
        // Step 4: verify code sent to NEW email
        [HttpPost("verify-email-change/new")]
        public async Task<IActionResult> VerifyEmailChangeNew([FromBody] VerifyEmailChangeRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                await _userService.VerifyNewEmailCodeAsync(userId.Value, req.RequestId, req.Code);
                return Ok(new { message = "Xác thực email mới thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xác thực mã email mới.", detail = ex.Message });
            }
        }
        // Step 5: complete email change (only when both verified)
        [HttpPost("complete-email-change")]
        public async Task<IActionResult> CompleteEmailChange([FromBody] CompleteEmailChangeRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                await _userService.CompleteEmailChangeAsync(userId.Value, req.RequestId);
                return Ok(new { message = "Thay đổi email thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi hoàn tất đổi email.", detail = ex.Message });
            }
        }
    }
}
