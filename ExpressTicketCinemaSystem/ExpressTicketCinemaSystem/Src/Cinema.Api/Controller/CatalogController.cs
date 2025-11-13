using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;
using RT = ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime;
using InfraRT = ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/cinema")]
    [Produces("application/json")]
    public class CatalogController : ControllerBase
    {

        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
        private readonly IShowtimeSeatEventStream _eventStream;
        private readonly ICatalogQueryService _service;

        public CatalogController(
             ICatalogQueryService service,
             IShowtimeSeatEventStream eventStream)
        {
            _service = service;
            _eventStream = eventStream;
        }

        /// <summary>
        /// Overview rạp → phòng → giờ chiếu của 1 phim theo ngày, có filter + phân trang rạp.
        /// Query: date=yyyy-MM-dd&city=&district=&brand=&cinemaId=&screenType=&formatType=&timeFrom=HH:mm&timeTo=HH:mm&page=1&limit=10&sortBy=time|cinema|brand&sortOrder=asc|desc
        /// </summary>
        [HttpGet("movies/{movieId:int}/showtimes/overview")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<MovieShowtimesOverviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMovieShowtimesOverview(
            int movieId,
            [FromQuery] GetMovieShowtimesOverviewQuery query,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetMovieShowtimesOverviewAsync(movieId, query, ct);
                return Ok(new SuccessResponse<MovieShowtimesOverviewResponse>
                {
                    Message = "Lấy danh sách suất chiếu thành công",
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
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy suất chiếu." });
            }
        }

        /// <summary>
        /// Sơ đồ ghế của 1 suất chiếu (AVAILABLE|LOCKED|SOLD|BLOCKED).
        /// </summary>
        [HttpGet("showtimes/{showtimeId:int}/seats")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<ShowtimeSeatMapResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetShowtimeSeats(int showtimeId, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetShowtimeSeatMapAsync(showtimeId, ct);
                return Ok(new SuccessResponse<ShowtimeSeatMapResponse>
                {
                    Message = "Lấy sơ đồ ghế thành công",
                    Result = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy sơ đồ ghế." });
            }
        }

        /// <summary>
        /// (SSE) Stream trạng thái ghế realtime cho 1 suất chiếu.
        /// - Sự kiện: snapshot | seat_locked | seat_released | seat_sold | heartbeat
        /// - Giữ kết nối lâu: client nên tự reconnect khi mất kết nối.
        /// </summary>
        [HttpGet("showtimes/{showtimeId:int}/seats/stream")]
        [AllowAnonymous]
        public async Task GetShowtimeSeatsStream(int showtimeId, CancellationToken ct)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no"; // nginx

            // 1) Gửi ảnh chụp ban đầu
            var snap = await _service.GetShowtimeSeatMapAsync(showtimeId, ct);
            var payload = new RT.SnapshotPayload
            {
                ServerTime = snap.ServerTime,
                Seats = snap.Seats.Select(s => new RT.SeatCell
                {
                    SeatId = s.SeatId,
                    RowCode = s.RowCode,
                    SeatNumber = s.SeatNumber,
                    SeatTypeId = s.SeatTypeId,
                    Status = s.Status,
                    LockedUntil = s.LockedUntil
                }).ToList()
            };
            await WriteSseAsync("snapshot", payload, ct);

            // 2) Heartbeat định kỳ
            var hbCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = Task.Run(async () =>
            {
                while (!hbCts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(HeartbeatInterval, hbCts.Token);
                        await WriteSseAsync("heartbeat", new HeartbeatPayload
                        {
                            ServerTime = DateTime.UtcNow
                        }, hbCts.Token);
                    }
                    catch { /* ignore */ }
                }
            }, hbCts.Token);

            // 3) Stream delta từ broker với error handling
            try
            {
                await foreach (var ev in _eventStream.SubscribeAsync(showtimeId, ct))
                {
                    try
                    {
                        switch (ev.Type)
                        {
                            case InfraRT.SeatEventType.Locked:
                                await WriteSseAsync("seat_locked", new RT.SeatDeltaPayload
                                {
                                    SeatId = ev.SeatId,
                                    LockedUntil = ev.LockedUntil
                                }, ct);
                                break;

                            case InfraRT.SeatEventType.Released:
                                await WriteSseAsync("seat_released", new RT.SeatDeltaPayload
                                {
                                    SeatId = ev.SeatId,
                                    LockedUntil = null
                                }, ct);
                                break;

                            case InfraRT.SeatEventType.Sold:
                                await WriteSseAsync("seat_sold", new { seatId = ev.SeatId }, ct);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // ✅ Log lỗi nhưng tiếp tục stream - không break connection
                        // Có thể gửi error event cho client nếu cần
                        try
                        {
                            await WriteSseAsync("error", new { message = "Lỗi xử lý sự kiện", seatId = ev.SeatId }, ct);
                        }
                        catch { /* ignore - connection có thể đã đóng */ }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ✅ Client disconnect - normal case, không cần log
            }
            catch (Exception)
            {
                // ✅ Lỗi stream - gửi error event cho client
                try
                {
                    await WriteSseAsync("error", new { message = "Lỗi kết nối stream" }, ct);
                }
                catch { /* ignore */ }
            }
            finally
            {
                hbCts.Cancel();
            }
        }
        /// <summary>
        /// Lấy danh sách suất chiếu theo rạp (cho màn home)
        /// </summary>
        [HttpGet("showtimes/by-cinema")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessResponse<CinemaShowtimesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCinemaShowtimes(
            [FromQuery] GetCinemaShowtimesQuery query,
            CancellationToken ct)
        {
            try
            {
                var result = await _service.GetCinemaShowtimesAsync(query, ct);
                return Ok(new SuccessResponse<CinemaShowtimesResponse>
                {
                    Message = "Lấy danh sách suất chiếu theo rạp thành công",
                    Result = result
                });
            }
            catch (ValidationException ex)
            {
                var msg = ex.Errors.Values.FirstOrDefault()?.Msg ?? "Lỗi xác thực dữ liệu";
                return BadRequest(new ValidationErrorResponse { Message = msg, Errors = ex.Errors });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Đã xảy ra lỗi hệ thống khi lấy danh sách suất chiếu." });
            }
        }

        // ===== Helpers =====
        private async Task WriteSseAsync(string @event, object data, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(data);
            var sb = new StringBuilder();
            sb.Append("event: ").Append(@event).Append('\n');
            sb.Append("data: ").Append(json).Append("\n\n");
            await Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()), ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
