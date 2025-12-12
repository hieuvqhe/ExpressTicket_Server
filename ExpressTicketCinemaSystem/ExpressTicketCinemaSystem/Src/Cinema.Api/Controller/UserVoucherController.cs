using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/user/vouchers")]
    [Authorize(Roles = "User")]
    [Produces("application/json")]
    public class UserVoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public UserVoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
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
        /// Get all user voucher
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SuccessResponse<List<UserVoucherResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetValidVouchers()
        {
            try
            {
                var userId = GetCurrentUserId();
                var vouchers = await _voucherService.GetValidVouchersForUserAsync();

                var response = new SuccessResponse<List<UserVoucherResponse>>
                {
                    Message = $"Tìm thấy {vouchers.Count} voucher hợp lệ",
                    Result = vouchers
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
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách voucher"
                });
            }
        }

        /// <summary>
        /// Get User voucher by voucher Code
        /// </summary>
        /// <param name="voucherCode">Voucher Code</param>
        [HttpGet("{voucherCode}")]
        [ProducesResponseType(typeof(SuccessResponse<UserVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVoucherByCode(string voucherCode)
        {
            try
            {
                var userId = GetCurrentUserId();
                var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode);

                if (voucher == null)
                {
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy voucher hợp lệ" });
                }

                var response = new SuccessResponse<UserVoucherResponse>
                {
                    Message = "Lấy thông tin voucher thành công",
                    Result = voucher
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
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin voucher"
                });
            }
        }

        /// <summary>
        /// Get user voucher by ID
        /// </summary>
        /// <param name="voucherId">Voucher ID</param>
        [HttpGet("{voucherId:int}")]
        [ProducesResponseType(typeof(SuccessResponse<UserVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVoucherById(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var voucher = await _voucherService.GetVoucherByIdForUserAsync(voucherId);

                if (voucher == null)
                {
                    return NotFound(new ErrorResponse { Message = "Không tìm thấy voucher hợp lệ" });
                }

                var response = new SuccessResponse<UserVoucherResponse>
                {
                    Message = "Lấy thông tin voucher thành công",
                    Result = voucher
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
            catch (UnauthorizedException ex)
            {
                var firstErrorMessage = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xác thực thất bại";
                return Unauthorized(new ValidationErrorResponse
                {
                    Message = firstErrorMessage,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin voucher"
                });
            }
        }
    }
}