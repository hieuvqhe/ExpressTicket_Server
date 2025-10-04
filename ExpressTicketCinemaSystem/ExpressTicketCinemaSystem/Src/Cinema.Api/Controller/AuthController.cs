using ExpressTicketCinemaSystem.Src.Cinema.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth;

namespace SEP490_G163.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }
        // Đăng ký tài khoản
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                AppUser user = await _authService.RegisterAsync(request);
                return Ok(new
                {
                    message = "Registration successful. Please verify your email.",
                    user = new
                    {
                        user.FullName,
                        user.Username,
                        user.Email,
                        user.EmailConfirmed
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                await _authService.VerifyEmailAsync(token);
                return Ok(new { message = "Xác minh email thành công." }); // verify mail qua token
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("resend-verification")]  // API Resend mail khi nguoi dung khong xac minh
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
    }

    public class ResendEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
