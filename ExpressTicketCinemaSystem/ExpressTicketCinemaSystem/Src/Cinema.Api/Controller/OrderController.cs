using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Payment.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Api.Filters;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private readonly CinemaDbCoreContext _db;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            CinemaDbCoreContext db,
            ILogger<OrderController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ============================================================
        // EXPIRE ORDER
        // ============================================================
        /// <summary>
        /// API đánh dấu một Order đang PENDING là đã hết hạn thanh toán: cập nhật trạng thái EXPIRED và giải phóng toàn bộ ghế đã lock.
        /// </summary>
        [HttpPost("{order_id}/expire")]
        [AuditAction("ORDER_EXPIRE", "Order", recordIdRouteKey: "order_id")]
        public async Task<IActionResult> ExpireOrder(
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
                return BadRequest(new ErrorResponse 
                { 
                    Message = $"Order đã ở trạng thái {order.Status}, không thể expire" 
                });
            }

            using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Update order status
                order.Status = "EXPIRED";
                order.UpdatedAt = DateTime.UtcNow;

                // Update booking session state back to DRAFT
                if (order.BookingSession != null && order.BookingSession.State == "PENDING_PAYMENT")
                {
                    order.BookingSession.State = "DRAFT";
                    order.BookingSession.UpdatedAt = DateTime.UtcNow;
                }

                // Release seat locks
                var seatLocks = await _db.SeatLocks
                    .Where(l => l.LockedBySession == order.BookingSessionId)
                    .ToListAsync(ct);
                
                _db.SeatLocks.RemoveRange(seatLocks);

                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Order {OrderId} expired successfully", order.OrderId);

                var response = new ExpireOrderResponse
                {
                    OrderId = order.OrderId,
                    Status = "EXPIRED",
                    ExpiredAt = order.UpdatedAt ?? DateTime.UtcNow,
                    Message = "Order đã hết hạn thanh toán"
                };

                return Ok(new SuccessResponse<ExpireOrderResponse>
                {
                    Message = "Order đã được đánh dấu hết hạn",
                    Result = response
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error expiring Order {OrderId}", order.OrderId);
                throw;
            }
        }
    }
}

