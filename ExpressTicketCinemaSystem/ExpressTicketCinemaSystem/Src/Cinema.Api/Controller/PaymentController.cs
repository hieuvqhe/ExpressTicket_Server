using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/payments/payos")]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly CinemaDbCoreContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPayOSService payOSService,
            CinemaDbCoreContext db,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _payOSService = payOSService;
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        // ============================================================
        // 1. CREATE PAYMENT
        // ============================================================
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePayOSPaymentRequest request,
            CancellationToken ct)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(
                x => x.OrderId == request.OrderId, ct);

            if (order == null)
                return NotFound(new ErrorResponse { Message = "Order không tồn tại" });

            if (order.Status != "PENDING")
                return BadRequest(new ErrorResponse { Message = "Order không còn ở trạng thái PENDING" });

            var result = await _payOSService.CreatePaymentAsync(
                order.OrderId,
                (long)order.Amount,
                $"Order {order.OrderId[..8]}",
                request.ReturnUrl ?? _configuration["PayOS:ReturnUrl"]!,
                request.CancelUrl ?? _configuration["PayOS:CancelUrl"]!,
                ct
            );

            order.PayOsOrderCode = result.ProviderRef;
            order.PayOsPaymentLink = result.CheckoutUrl;
            order.PayOsQrCode = result.QrCode;
            order.PaymentExpiresAt = DateTime.UtcNow.AddMinutes(15);
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return Ok(new SuccessResponse<CreatePayOSPaymentResponse>
            {
                Message = "Tạo payment thành công",
                Result = new CreatePayOSPaymentResponse
                {
                    OrderId = order.OrderId,
                    CheckoutUrl = result.CheckoutUrl,
                    ProviderRef = result.ProviderRef,
                    QrCode = result.QrCode,
                    ExpiresAt = order.PaymentExpiresAt
                }
            });
        }

        // ============================================================
        // 3. HANDLE PAYOS RETURN URL
        // ============================================================
        /// <summary>
        /// Endpoint để xử lý returnUrl từ PayOS sau khi thanh toán
        /// PayOS redirect về: /payment/return?code=00&id=xxx&status=PAID&orderCode=xxx
        /// </summary>
        [HttpGet("return")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleReturnUrl(
            [FromQuery] string? id, // PayOS payment link ID
            [FromQuery] string? orderCode, // PayOS order code (numeric)
            [FromQuery] string? code, // "00" = success
            [FromQuery] string? status, // "PAID", "CANCELLED", etc.
            CancellationToken ct = default)
        {
            _logger.LogInformation("PayOS return URL called: id={Id}, orderCode={OrderCode}, code={Code}, status={Status}",
                id, orderCode, code, status);

            // Tìm Order bằng PayOsOrderCode (id trong URL)
            Order? order = null;
            if (!string.IsNullOrEmpty(id))
            {
                order = await _db.Orders
                    .Include(o => o.BookingSession)
                    .FirstOrDefaultAsync(x => x.PayOsOrderCode == id, ct);
            }

            if (order == null && !string.IsNullOrEmpty(orderCode))
            {
                // Thử tìm bằng orderCode (numeric) - cần convert
                order = await _db.Orders
                    .Include(o => o.BookingSession)
                    .FirstOrDefaultAsync(x => x.PayOsOrderCode == orderCode, ct);
            }

            if (order == null)
            {
                _logger.LogWarning("Order not found for PayOS return: id={Id}, orderCode={OrderCode}", id, orderCode);
                // Redirect về frontend với error
                return Redirect($"{_configuration["AppSettings:FrontendUrl"]}/payment/return?error=order_not_found");
            }

            // Nếu đã PAID rồi, redirect về success
            if (order.Status == "PAID")
            {
                _logger.LogInformation("Order {OrderId} already PAID, redirecting to success", order.OrderId);
                return Redirect($"{_configuration["AppSettings:FrontendUrl"]}/payment/success?orderId={order.OrderId}");
            }

            // Nếu code=00 và status=PAID, xử lý payment
            if (code == "00" && status == "PAID" && order.Status == "PENDING")
            {
                try
                {
                    _logger.LogInformation("Processing payment for Order {OrderId} from return URL", order.OrderId);

                    // Gọi PayOS API để verify status
                    // PayOS API cần numeric orderCode (convert từ OrderId), không phải paymentLinkId
                    var payOSStatus = await _payOSService.GetPaymentStatusAsync(
                        order.OrderId, ct);

                    if (payOSStatus.Status == "PAID")
                    {
                        // Xử lý payment như webhook
                        var webhookRequest = new PayOSWebhookRequest
                        {
                            Code = "00",
                            Data = new PayOSWebhookData
                            {
                                OrderCode = order.PayOsOrderCode,
                                Amount = (long)order.Amount,
                                Status = "PAID"
                            }
                        };

                        await ProcessSuccessfulPaymentAsync(order, webhookRequest, ct);

                        _logger.LogInformation("Payment processed successfully for Order {OrderId}", order.OrderId);
                        return Redirect($"{_configuration["AppSettings:FrontendUrl"]}/payment/success?orderId={order.OrderId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment for Order {OrderId} from return URL", order.OrderId);
                    // Vẫn redirect về frontend, frontend sẽ check lại
                }
            }

            // Redirect về frontend với orderId để frontend check lại
            return Redirect($"{_configuration["AppSettings:FrontendUrl"]}/payment/return?orderId={order.OrderId}&code={code}&status={status}");
        }

        // ============================================================
        // 4. GET PAYMENT STATUS
        // ============================================================
        [HttpGet("status/{order_id}")]
        public async Task<IActionResult> GetPaymentStatus(
            [FromRoute] string order_id,
            CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(
                x => x.OrderId == order_id, ct);

            if (order == null)
                return NotFound(new ErrorResponse { Message = "Order không tồn tại" });

            // Nếu order đang PENDING, check status từ PayOS
            if (order.Status == "PENDING" && !string.IsNullOrEmpty(order.PayOsOrderCode))
            {
                try
                {
                    // PayOS API cần numeric orderCode (convert từ OrderId), không phải paymentLinkId
                    var payOSStatus = await _payOSService.GetPaymentStatusAsync(
                        order.OrderId, ct);

                    // Nếu PayOS báo PAID nhưng order chưa PAID, xử lý như webhook
                    if (payOSStatus.Status == "PAID" && order.Status == "PENDING")
                    {
                        _logger.LogInformation("Order {OrderId} found PAID in PayOS, processing payment...", order.OrderId);
                        
                        var webhookRequest = new PayOSWebhookRequest
                        {
                            Code = "00",
                            Data = new PayOSWebhookData
                            {
                                OrderCode = order.PayOsOrderCode,
                                Amount = (long)order.Amount,
                                Status = "PAID"
                            }
                        };

                        // Tạo fake webhook để xử lý
                        await ProcessSuccessfulPaymentAsync(order, webhookRequest, ct);
                    }
                    else if (payOSStatus.Status == "CANCELLED" || payOSStatus.Status == "EXPIRED")
                    {
                        order.Status = "EXPIRED";
                        order.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                    }

                    // Reload order để lấy status mới nhất
                    await _db.Entry(order).ReloadAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get PayOS status for Order {OrderId}, using local status", order.OrderId);
                }
            }

            var response = new PayOSPaymentStatusResponse
            {
                OrderId = order.OrderId,
                Status = order.Status,
                PaymentLink = order.PaymentExpiresAt > DateTime.UtcNow ? order.PayOsPaymentLink : null,
                QrCode = order.PaymentExpiresAt > DateTime.UtcNow ? order.PayOsQrCode : null,
                PaidAt = order.Status == "PAID" ? order.UpdatedAt : null,
                ExpiresAt = order.PaymentExpiresAt,
                ProviderRef = order.PayOsOrderCode
            };

            return Ok(new SuccessResponse<PayOSPaymentStatusResponse>
            {
                Message = "Lấy trạng thái thanh toán thành công",
                Result = response
            });
        }


        // ============================================================
        // PRIVATE HELPERS
        // ============================================================
        private async Task ProcessSuccessfulPaymentAsync(
            Order order,
            PayOSWebhookRequest request,
            CancellationToken ct)
        {
            // 1. Update Order status
            order.Status = "PAID";
            order.UpdatedAt = DateTime.UtcNow;

            // 2. Get or create Customer (nếu có UserId)
            int? customerId = null;
            if (order.UserId.HasValue)
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.UserId == order.UserId.Value, ct);

                if (customer == null)
                {
                    // Tạo Customer mới
                    customer = new Customer
                    {
                        UserId = order.UserId.Value,
                        LoyaltyPoints = 0
                    };
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Created new Customer {CustomerId} for User {UserId}", 
                        customer.CustomerId, order.UserId.Value);
                }

                customerId = customer.CustomerId;
            }
            else
            {
                // Anonymous user - không thể tạo Booking
                _logger.LogWarning("Order {OrderId} has no UserId, cannot create Booking", order.OrderId);
                await _db.SaveChangesAsync(ct);
                return;
            }

            // 3. Load BookingSession với items và pricing
            var session = await _db.BookingSessions
                .FirstOrDefaultAsync(s => s.Id == order.BookingSessionId, ct);

            if (session == null)
            {
                _logger.LogError("BookingSession {SessionId} not found for Order {OrderId}", 
                    order.BookingSessionId, order.OrderId);
                await _db.SaveChangesAsync(ct);
                return;
            }

            // 4. Parse items và pricing
            var (seats, combos) = ReadItems(session.ItemsJson);
            var pricing = ReadPricing(session.PricingJson);

            // 5. Tạo Booking
            var bookingCode = GenerateBookingCode();
            var booking = new Booking
            {
                CustomerId = customerId.Value,
                ShowtimeId = order.ShowtimeId,
                BookingCode = bookingCode,
                BookingTime = DateTime.UtcNow,
                TotalAmount = order.Amount,
                Status = "CONFIRMED",
                OrderCode = order.OrderId,
                SessionId = session.Id,
                PricingSnapshot = session.PricingJson,
                State = "COMPLETED",
                PaymentProvider = order.Provider,
                PaymentTxId = order.PayOsOrderCode,
                PaymentStatus = "PAID",
                VoucherId = await GetVoucherIdFromCouponCode(session.CouponCode, ct),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct); // Save để lấy BookingId

            // 6. Tạo Tickets cho mỗi seat
            var showtime = await _db.Showtimes
                .Include(s => s.Screen)
                .FirstOrDefaultAsync(s => s.ShowtimeId == order.ShowtimeId, ct);

            if (showtime != null && seats.Count > 0)
            {
                // Load tất cả seats và seat types một lần
                var seatList = await _db.Seats
                    .Include(s => s.SeatType)
                    .Where(s => seats.Contains(s.SeatId))
                    .ToListAsync(ct);

                foreach (var seat in seatList)
                {
                    // Tính giá ticket: base price + seat type surcharge
                    var surcharge = seat.SeatType?.Surcharge ?? 0m;
                    var ticketPrice = showtime.BasePrice + surcharge;

                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        ShowtimeId = order.ShowtimeId,
                        SeatId = seat.SeatId,
                        Price = ticketPrice,
                        Status = "ACTIVE"
                    };
                    _db.Tickets.Add(ticket);
                }
            }

            // 7. Tạo ServiceOrders cho mỗi combo
            var comboGroups = combos.GroupBy(c => c).ToList();
            foreach (var group in comboGroups)
            {
                var serviceId = group.Key;
                var quantity = group.Count();

                var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == serviceId, ct);
                if (service != null)
                {
                    var serviceOrder = new ServiceOrder
                    {
                        BookingId = booking.BookingId,
                        ServiceId = serviceId,
                        Quantity = quantity,
                        UnitPrice = service.Price
                    };
                    _db.ServiceOrders.Add(serviceOrder);
                }
            }

            // 8. Tạo Payment record
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Amount = order.Amount,
                Method = "ONLINE",
                Provider = order.Provider,
                TransactionId = order.PayOsOrderCode ?? order.OrderId,
                Status = "PAID",
                SignatureOk = true,
                PaidAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);

            // 9. Update BookingSession state
            session.State = "COMPLETED";
            session.UpdatedAt = DateTime.UtcNow;

            // 10. Release seat locks
            var seatLocks = await _db.SeatLocks
                .Where(l => l.LockedBySession == session.Id)
                .ToListAsync(ct);
            _db.SeatLocks.RemoveRange(seatLocks);

            // 11. Update Order với BookingId
            order.BookingId = booking.BookingId;

            // 12. Save all changes
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Payment processed successfully - OrderId: {OrderId}, BookingId: {BookingId}, Tickets: {TicketCount}, ServiceOrders: {ServiceOrderCount}",
                order.OrderId, booking.BookingId, seats.Count, comboGroups.Count);
        }

        private static (List<int> seats, List<int> combos) ReadItems(string? itemsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemsJson)) return (new(), new());
                using var doc = JsonDocument.Parse(itemsJson);
                var root = doc.RootElement;

                var seats = root.TryGetProperty("seats", out var sEl) && sEl.ValueKind == JsonValueKind.Array
                    ? sEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                var combos = root.TryGetProperty("combos", out var cEl) && cEl.ValueKind == JsonValueKind.Array
                    ? cEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                return (seats, combos);
            }
            catch { return (new(), new()); }
        }

        private static object ReadPricing(string? pricingJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pricingJson)) return new { };
                return JsonSerializer.Deserialize<object>(pricingJson) ?? new { };
            }
            catch { return new { }; }
        }

        private async Task<int?> GetVoucherIdFromCouponCode(string? couponCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
                return null;

            var voucher = await _db.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == couponCode, ct);

            return voucher?.VoucherId;
        }

        private string GenerateBookingCode()
        {
            // Generate unique booking code: BK + timestamp + random
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"BK{timestamp}{random}";
        }

        // ============================================================
        // 4. CHECK PAYMENT STATUS (Trigger check ngay - dùng khi không có webhook)
        // ============================================================
        /// <summary>
        /// Endpoint để frontend trigger check payment status ngay sau khi return từ PayOS
        /// Thay thế webhook cho tài khoản PayOS cá nhân không có webhook
        /// </summary>
        [HttpPost("check/{order_id}")]
        public async Task<IActionResult> CheckPaymentStatus(
            [FromRoute] string order_id,
            CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.BookingSession)
                .FirstOrDefaultAsync(x => x.OrderId == order_id, ct);

            if (order == null)
                return NotFound(new ErrorResponse { Message = "Order không tồn tại" });

            if (order.Status != "PENDING")
            {
                // Order đã được xử lý rồi
                var statusResponse = new PayOSPaymentStatusResponse
                {
                    OrderId = order.OrderId,
                    Status = order.Status,
                    PaymentLink = null,
                    QrCode = null,
                    PaidAt = order.Status == "PAID" ? order.UpdatedAt : null,
                    ExpiresAt = order.PaymentExpiresAt,
                    ProviderRef = order.PayOsOrderCode
                };

                return Ok(new SuccessResponse<PayOSPaymentStatusResponse>
                {
                    Message = $"Order đã ở trạng thái {order.Status}",
                    Result = statusResponse
                });
            }

            if (string.IsNullOrEmpty(order.PayOsOrderCode))
            {
                return BadRequest(new ErrorResponse { Message = "Order chưa có payment link" });
            }

            try
            {
                // Gọi PayOS API để check status
                // PayOS API cần numeric orderCode (convert từ OrderId), không phải paymentLinkId
                // Dùng OrderId để convert thành numeric orderCode (giống như khi tạo payment)
                var payOSStatus = await _payOSService.GetPaymentStatusAsync(
                    order.OrderId, ct);

                // Nếu PayOS báo PAID nhưng Order chưa PAID, xử lý như webhook
                if (payOSStatus.Status == "PAID" && order.Status == "PENDING")
                {
                    _logger.LogInformation("Order {OrderId} found PAID in PayOS, processing payment...", order.OrderId);
                    
                    var webhookRequest = new PayOSWebhookRequest
                    {
                        Code = "00",
                        Data = new PayOSWebhookData
                        {
                            OrderCode = order.PayOsOrderCode,
                            Amount = (long)order.Amount,
                            Status = "PAID"
                        }
                    };

                    // Xử lý payment
                    await ProcessSuccessfulPaymentAsync(order, webhookRequest, ct);
                }
                else if (payOSStatus.Status == "CANCELLED" || payOSStatus.Status == "EXPIRED")
                {
                    order.Status = "EXPIRED";
                    order.UpdatedAt = DateTime.UtcNow;

                    if (order.BookingSession != null && order.BookingSession.State == "PENDING_PAYMENT")
                    {
                        order.BookingSession.State = "DRAFT";
                        order.BookingSession.UpdatedAt = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync(ct);
                }

                // Reload order để lấy status mới nhất
                await _db.Entry(order).ReloadAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check PayOS status for Order {OrderId}", order.OrderId);
                // Vẫn trả về status hiện tại
            }

            var response = new PayOSPaymentStatusResponse
            {
                OrderId = order.OrderId,
                Status = order.Status,
                PaymentLink = order.PaymentExpiresAt > DateTime.UtcNow ? order.PayOsPaymentLink : null,
                QrCode = order.PaymentExpiresAt > DateTime.UtcNow ? order.PayOsQrCode : null,
                PaidAt = order.Status == "PAID" ? order.UpdatedAt : null,
                ExpiresAt = order.PaymentExpiresAt,
                ProviderRef = order.PayOsOrderCode
            };

            return Ok(new SuccessResponse<PayOSPaymentStatusResponse>
            {
                Message = "Đã kiểm tra trạng thái thanh toán",
                Result = response
            });
        }

    }
}
