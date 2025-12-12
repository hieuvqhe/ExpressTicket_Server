using Microsoft.AspNetCore.Mvc;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
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

        /// <summary>
        /// Register a new user account
        /// </summary>
        [HttpPost("register")]
        [AuditAction("AUTH_REGISTER", "User", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request);

                var response = new SuccessResponse<object>
                {
                    Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác minh.",
                    Result = new
                    {
                        user.Fullname,
                        user.Username,
                        user.Email,
                        user.EmailConfirmed
                    }
                };

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Dữ liệu bị xung đột",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đăng ký.",
                });
            }
        }

        /// <summary>
        /// Email Verification
        /// </summary>
        [HttpGet("verify-email")]
        [AuditAction("AUTH_VERIFY_EMAIL", "User")]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                await _authService.VerifyEmailAsync(token);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Xác minh email thành công.",
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Xác thực thất bại",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình xác minh email.",
                });
            }
        }

        /// <summary>
        /// Resend verification email
        /// </summary>
        [HttpPost("resend-verification")]
        [AuditAction("AUTH_RESEND_VERIFICATION", "User", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResendVerification([FromBody] ResendEmailRequest request)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(request.Email);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Email xác minh đã được gửi lại.",
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
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Dữ liệu bị xung đột",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình gửi lại email xác minh.",
                });
            }
        }

        /// <summary>
        /// Login
        /// </summary>
        [HttpPost("login")]
        [AuditAction("AUTH_LOGIN", "UserAuth", includeRequestBody: true)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);

                var successResponse = new SuccessResponse<LoginResponse>
                {
                    Message = "Đăng nhập thành công",
                    Result = response
                };
                return Ok(successResponse);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực dữ liệu",
                    Errors = ex.Errors
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đăng nhập.",
                });
            }
        }

        /// <summary>
        /// Sign in with Google
        /// </summary>
        [HttpPost("login/google")]
        [AuditAction("AUTH_LOGIN_GOOGLE", "UserAuth", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["idToken"] = new ValidationError
                            {
                                Msg = "Thiếu idToken hoặc idToken không hợp lệ",
                                Path = "idToken",
                                Location = "body"
                            }
                        }
                    });
                }

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
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực Google",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["idToken"] = new ValidationError
                            {
                                Msg = "Token không thuộc về ứng dụng này",
                                Path = "idToken",
                                Location = "body"
                            }
                        }
                    });
                }

                if (!payload.EmailVerified)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi xác thực Google",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["email"] = new ValidationError
                            {
                                Msg = "Email chưa được xác thực với Google",
                                Path = "email",
                                Location = "body"
                            }
                        }
                    });
                }

                var user = await _context.Users.Include(u => u.Partner).FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Lỗi đăng nhập",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["email"] = new ValidationError
                            {
                                Msg = "Tài khoản chưa đăng ký với hệ thống",
                                Path = "email",
                                Location = "body"
                            }
                        }
                    });
                }

                if (!user.IsActive)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Tài khoản bị khóa",
                        Errors = new Dictionary<string, ValidationError>
                        {
                            ["account"] = new ValidationError
                            {
                                Msg = "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên",
                                Path = "account",
                                Location = "body"
                            }
                        }
                    });
                }

                if (!user.EmailConfirmed)
                {
                    // Tự động xác thực email nếu đăng nhập bằng Google thành công
                    user.EmailConfirmed = true;
                    await _context.SaveChangesAsync();
                }

                var response = await _authService.CreateJwtResponseAsync(user);

                var successResponse = new SuccessResponse<LoginResponse>
                {
                    Message = "Đăng nhập bằng Google thành công",
                    Result = response
                };
                return Ok(successResponse);
            }
            catch (Google.Apis.Auth.InvalidJwtException ex)
            {
                var errorMessage = ex.Message.Contains("expired")
                    ? "Token Google đã hết hạn, vui lòng đăng nhập lại"
                    : "Token Google không hợp lệ";

                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực Google",
                    Errors = new Dictionary<string, ValidationError>
                    {
                        ["idToken"] = new ValidationError
                        {
                            Msg = errorMessage,
                            Path = "idToken",
                            Location = "body"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đăng nhập Google.",
                });
            }
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [AuditAction("AUTH_REFRESH_TOKEN", "UserAuth", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var newToken = await _authService.RefreshTokenAsync(request.RefreshToken);

                var successResponse = new SuccessResponse<LoginResponse>
                {
                    Message = "Làm mới token thành công",
                    Result = newToken
                };
                return Ok(successResponse);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = "Xác thực thất bại",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình làm mới token.",
                });
            }
        }

        /// <summary>
        /// Logout
        /// </summary>
        [HttpPost("logout")]
        [AuditAction("AUTH_LOGOUT", "UserAuth")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await _authService.LogoutAsync(request.RefreshToken);

                return Ok(new SuccessResponse<object>
                {
                    Message = "Đăng xuất thành công",
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đăng xuất.",
                });
            }
        }

        /// <summary>
        /// Forgot password
        /// </summary>
        [HttpPost("forgot-password")]
        [AuditAction("AUTH_FORGOT_PASSWORD", "UserAuth", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Mã khôi phục đã được gửi đến email của bạn.",
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực",
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ValidationErrorResponse
                {
                    Message = "Xung đột dữ liệu",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình gửi mã khôi phục.",
                });
            }
        }

        /// <summary>
        /// Verify password reset code
        /// </summary>
        [HttpPost("verify-reset-code")]
        [AuditAction("AUTH_VERIFY_RESET_CODE", "UserAuth", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            try
            {
                await _authService.VerifyResetCodeAsync(request);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Mã hợp lệ, bạn có thể đặt lại mật khẩu mới.",
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình xác minh mã.",
                });
            }
        }

        [HttpPost("reset-password")]
        [AuditAction("AUTH_RESET_PASSWORD", "UserAuth", includeRequestBody: true)]
        [ProducesResponseType(typeof(SuccessResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new SuccessResponse<object>
                {
                    Message = "Đặt lại mật khẩu thành công, vui lòng đăng nhập lại.",
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Lỗi xác thực",
                    Errors = ex.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống trong quá trình đặt lại mật khẩu.",
                });
            }
        }
    }

    public class ResendEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}