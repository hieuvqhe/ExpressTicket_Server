using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/booking/sessions")]
    [Produces("application/json")]
    public class BookingSessionsController : ControllerBase
    {
        private readonly IBookingSessionService _service;
        private readonly IBookingSessionComboService _comboService;
        private readonly IBookingPricingService _pricingService;
        private readonly IBookingCheckoutService _checkoutService;
        private readonly IBookingExtrasService _extras;

        public BookingSessionsController(IBookingSessionService service , IBookingSessionComboService comboService , IBookingPricingService pricingService,
        IBookingCheckoutService checkoutService , IBookingExtrasService extras)
        {
            _service = service;
            _comboService = comboService;
            _pricingService = pricingService;
            _checkoutService = checkoutService;
            _extras = extras;
        }

        /// <summary>
        /// Tạo booking session (DRAFT) với TTL ~10 phút. Cho phép anonymous.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingSessionRequest request,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.CreateAsync(User, request, ct);

                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Tạo session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse
                {
                    Message = msg,
                    Errors = ex.Errors
                });
            }
            catch (ConflictException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Xung đột dữ liệu";
                return StatusCode(StatusCodes.Status409Conflict, new ValidationErrorResponse
                {
                    Message = msg,
                    Errors = ex.Errors
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi tạo session."
                });
            }
        }
        /// <summary>
        /// Lấy chi tiết session (items, pricing, state, TTL). Cho phép anonymous.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAsync(id, ct);
                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Lấy thông tin session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi lấy thông tin session."
                });
            }
        }

        /// <summary>
        /// Gia hạn TTL session (~10') và seat locks (~3'). Cho phép anonymous.
        /// </summary>
        [HttpPost("{id:guid}/touch")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<BookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Touch(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _service.TouchAsync(id, ct);
                return Ok(new SuccessResponse<BookingSessionResponse>
                {
                    Message = "Gia hạn session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi gia hạn session."
                });
            }
        }
        /// <summary>
        /// Hủy session và giải phóng toàn bộ ghế đang giữ ngay lập tức.
        /// Trạng thái chuyển thành CANCELED và gửi SSE seat_released.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<CancelBookingSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _service.CancelAsync(id, ct);
                return Ok(new SuccessResponse<CancelBookingSessionResponse>
                {
                    Message = "Hủy session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Đã xảy ra lỗi hệ thống khi hủy session."
                });
            }
        }
        /// <summary>
        /// Lấy danh sách combo khả dụng cho session (anonymous được).
        /// Nếu user đã đăng nhập: trả thêm danh sách voucher hợp lệ và gợi ý auto-voucher,
        /// đồng thời tính sẵn PriceAfterAutoDiscount cho từng combo.
        /// </summary>
        [HttpGet("{id:guid}/combos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<GetSessionCombosResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSessionCombos(Guid id, [FromServices] IBookingComboQueryService comboQuery, CancellationToken ct)
        {
            try
            {
                var result = await comboQuery.GetSessionCombosAsync(id, User, ct);
                return Ok(new SuccessResponse<GetSessionCombosResponse>
                {
                    Message = "Lấy danh sách combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách combo." });
            }
        }
        /// <summary>Upsert danh sách combo cho session (replace theo quantity, tối đa 8 đơn vị). Cho phép anonymous.</summary>
        [HttpPost("{id:guid}/combos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<UpsertSessionCombosResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertCombos(Guid id, [FromBody] UpsertSessionCombosRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _comboService.UpsertCombosAsync(id, request, ct);
                return Ok(new SuccessResponse<UpsertSessionCombosResponse>
                {
                    Message = "Cập nhật combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi cập nhật combo." });
            }
        }
        /// <summary>Thay thế toàn bộ danh sách combo của session (replace all). Cho phép anonymous. Rule: tổng <= 8 đơn vị.</summary>
        [HttpPut("{id:guid}/combos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<UpdateCombosResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReplaceCombos(Guid id, [FromBody] UpdateCombosRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _extras.ReplaceCombosAsync(id, request, ct);
                return Ok(new SuccessResponse<UpdateCombosResponse>
                {
                    Message = "Cập nhật danh sách combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi cập nhật combo." });
            }
        }

        /// <summary>Xóa nhanh 1 loại combo khỏi session (xóa toàn bộ units của serviceId). Cho phép anonymous.</summary>
        [HttpDelete("{id:guid}/combos/{serviceId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<RemoveComboResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCombo(Guid id, int serviceId, CancellationToken ct)
        {
            try
            {
                var result = await _extras.RemoveComboAsync(id, serviceId, ct);
                return Ok(new SuccessResponse<RemoveComboResponse>
                {
                    Message = "Xóa combo thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi xóa combo." });
            }
        }

        /// <summary>Xem trước giá (seats + combos). Tùy chọn voucherCode (yêu cầu đã đăng nhập).</summary>
        [HttpPost("{id:guid}/pricing/preview")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<PricingPreviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PricingPreview(Guid id, [FromBody] PricingPreviewRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _comboService.PreviewPricingAsync(id, User, request, ct);
                return Ok(new SuccessResponse<PricingPreviewResponse>
                {
                    Message = "Tính giá tạm tính thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi tính giá." });
            }
        }
        /// <summary>Áp dụng voucher cho session (yêu cầu đăng nhập).</summary>
        [HttpPost("{id:guid}/pricing/apply-coupon")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(SuccessResponse<ApplyCouponResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApplyCoupon(Guid id, [FromBody] ApplyCouponRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _pricingService.ApplyCouponAsync(id, User, request, ct);
                return Ok(new SuccessResponse<ApplyCouponResponse>
                {
                    Message = "Áp dụng voucher thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi áp dụng voucher." });
            }
        }
        /// <summary>Set/replace voucher cho session. Yêu cầu user đăng nhập.</summary>
        [HttpPut("{id:guid}/voucher")]
        [Authorize] // hoặc [Authorize(Roles="User")] nếu bạn phân quyền
        [ProducesResponseType(typeof(SuccessResponse<SetVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetVoucher(Guid id, [FromBody] SetVoucherRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _extras.SetVoucherAsync(id, request.VoucherCode, ct);
                return Ok(new SuccessResponse<SetVoucherResponse>
                {
                    Message = "Áp dụng voucher cho session thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi áp dụng voucher." });
            }
        }

        /// <summary>Gỡ voucher khỏi session. Yêu cầu user đăng nhập.</summary>
        [HttpDelete("{id:guid}/voucher")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse<RemoveVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveVoucher(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _extras.RemoveVoucherAsync(id, ct);
                return Ok(new SuccessResponse<RemoveVoucherResponse>
                {
                    Message = "Đã gỡ voucher",
                    Result = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ValidationErrorResponse { Message = ex.Message, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi gỡ voucher." });
            }
        }

        /// <summary>Khởi tạo thanh toán: chuyển session → PENDING_PAYMENT và giữ ghế đến khi thanh toán.</summary>
        [HttpPost("{id:guid}/checkout")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<CheckoutResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout(Guid id, [FromBody] CheckoutRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _checkoutService.CheckoutAsync(id, request, ct);
                return Ok(new SuccessResponse<CheckoutResponse>
                {
                    Message = result.Message,
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi khởi tạo thanh toán." });
            }
        }
    }
}
