using Microsoft.AspNetCore.Mvc;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests;
namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly CinemaDbCoreContext _context;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, CinemaDbCoreContext context, IConfiguration config)
        {
            _authService = authService;
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(RegisterSuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            try
            {
                var user = await _authService.RegisterAsync(request);

                var successResponse = new RegisterSuccessResponse
                {
                    Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh.",
                    User = new
                    {
                        user.Fullname,
                        user.Username,
                        user.Email,
                        user.EmailConfirmed
                    }
                };

                return Ok(successResponse);
            }
            catch (ArgumentException ex) 
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex) 
            {

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse { Message = "An unexpected error occurred on the server." });
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                await _authService.VerifyEmailAsync(token);
                return Ok(new { message = "Xác minh email thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendEmailRequest request)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(request.Email);
                return Ok(new { message = "Email xác minh đã được gửi lại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(new { message = "Đăng nhập thành công.", data = response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng nhập." });
            }
        }
        [HttpPost("login/google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                    throw new ArgumentException("Thiếu idToken hoặc idToken không hợp lệ.");

                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings()
                );

                var validClientIds = new List<string>
        {
            _config["Authentication:Google:ClientId"], 
            "407408718192.apps.googleusercontent.com" 
        };

                if (payload.Audience == null || !validClientIds.Contains(payload.Audience.ToString()))
                {
                    throw new UnauthorizedAccessException("Token không thuộc về ứng dụng này (audience mismatch).");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Fullname = payload.Name,
                        Email = payload.Email,
                        EmailConfirmed = true,
                        Username = payload.Email.Split('@')[0],
                        UserType = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                if (!user.IsActive)
                    throw new UnauthorizedAccessException("Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.");

                var response = await _authService.CreateJwtResponseAsync(user);
                return Ok(new { message = "Đăng nhập bằng Google thành công.", data = response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Google.Apis.Auth.InvalidJwtException ex)
            {
                if (ex.Message.Contains("expired"))
                    return Unauthorized(new { message = "Token Google đã hết hạn, vui lòng đăng nhập lại." });
                return Unauthorized(new { message = "Token Google không hợp lệ." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng nhập." });
            }
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequest(new { message = "Thiếu refreshToken." });

                var newToken = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (newToken == null)
                    return Unauthorized(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn." });

                return Ok(new
                {
                    message = "Làm mới token thành công.",
                    data = newToken
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình làm mới token." });
            }
        }
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequest(new { message = "Thiếu refreshToken." });

                var result = await _authService.LogoutAsync(request.RefreshToken);

                if (!result)
                    return NotFound(new { message = "Refresh token không tồn tại hoặc đã được hủy." });

                return Ok(new { message = "Đăng xuất thành công." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng xuất." });
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);
                return Ok(new { message = "Mã khôi phục đã được gửi đến email của bạn." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình gửi mã khôi phục." });
            }
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            try
            {
                await _authService.VerifyResetCodeAsync(request);
                return Ok(new { message = "Mã hợp lệ, bạn có thể đặt lại mật khẩu mới." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Mã không hợp lệ hoặc đã hết hạn." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Đặt lại mật khẩu thành công, vui lòng đăng nhập lại." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đặt lại mật khẩu." });
            }
        }

    }

    public class ResendEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
