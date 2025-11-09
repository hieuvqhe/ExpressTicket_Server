using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using InfraRT = ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IBookingSessionService
    {
        Task<BookingSessionResponse> CreateAsync(
            ClaimsPrincipal? user,
            CreateBookingSessionRequest request,
            CancellationToken ct = default);

        Task<BookingSessionResponse> GetAsync(
            Guid bookingSessionId,
            CancellationToken ct = default);

        Task<BookingSessionResponse> TouchAsync(
            Guid bookingSessionId,
            CancellationToken ct = default);

        Task<CancelBookingSessionResponse> CancelAsync(Guid bookingSessionId, CancellationToken ct = default);
    }

    public class BookingSessionService : IBookingSessionService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly InfraRT.IShowtimeSeatEventStream _eventStream;

        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SeatLockTtl = TimeSpan.FromMinutes(3);

        public BookingSessionService(CinemaDbCoreContext db, InfraRT.IShowtimeSeatEventStream eventStream)
        {
            _db = db;
            _eventStream = eventStream;
        }

        private static int? GetUserId(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value
                       ?? user.FindFirst("user_id")?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        public async Task<BookingSessionResponse> CreateAsync(
            ClaimsPrincipal? user,
            CreateBookingSessionRequest request,
            CancellationToken ct = default)
        {
            if (request.ShowtimeId <= 0)
                throw new ValidationException("showtimeId", "ShowtimeId không hợp lệ");

            // 1) Showtime phải tồn tại và còn hiệu lực
            var showtime = await _db.Showtimes
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x => x.ShowtimeId == request.ShowtimeId, ct);
            if (showtime == null)
                throw new NotFoundException("Showtime không tồn tại");

            if (showtime.EndTime != null && showtime.EndTime <= DateTime.UtcNow)
                throw new ConflictException("showtimeId", "Showtime đã kết thúc");

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(SessionTtl);
            var userId = GetUserId(user);

            // 2) Idempotency “thân thiện”: nếu user đã có 1 session DRAFT cùng showtime & còn sống → trả lại
            var idemKey = (user?.Identities?.FirstOrDefault()?.Claims
                            ?.FirstOrDefault(c => c.Type == "Idempotency-Key")?.Value)
                          ?? string.Empty; // không bắt buộc có

            if (userId.HasValue)
            {
                var existing = await _db.BookingSessions
                    .AsNoTracking()
                    .Where(b => b.UserId == userId.Value
                                && b.ShowtimeId == request.ShowtimeId
                                && b.State == "DRAFT"
                                && b.ExpiresAt > now)
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (existing != null)
                {
                    return new BookingSessionResponse
                    {
                        BookingSessionId = existing.Id,
                        State = existing.State,
                        ShowtimeId = existing.ShowtimeId,
                        Items = JsonSerializer.Deserialize<object>(existing.ItemsJson!)!,
                        Pricing = JsonSerializer.Deserialize<object>(existing.PricingJson!)!,
                        ExpiresAt = existing.ExpiresAt,
                        Version = existing.Version
                    };
                }
            }

            // 3) Tạo session mới
            var entity = new BookingSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShowtimeId = request.ShowtimeId,
                ItemsJson = @"{""seats"":[],""combos"":[]}",
                PricingJson = @"{""subtotal"":0,""discount"":0,""fees"":0,""total"":0,""currency"":""VND""}",
                State = "DRAFT",
                ExpiresAt = expiresAt,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.BookingSessions.Add(entity);
            await _db.SaveChangesAsync(ct);

            return new BookingSessionResponse
            {
                BookingSessionId = entity.Id,
                State = entity.State,
                ShowtimeId = entity.ShowtimeId,
                Items = new { seats = Array.Empty<object>(), combos = Array.Empty<object>() },
                Pricing = new { subtotal = 0, discount = 0, fees = 0, total = 0, currency = "VND" },
                ExpiresAt = entity.ExpiresAt,
                Version = entity.Version
            };
        }
        public async Task<BookingSessionResponse> GetAsync(Guid bookingSessionId, CancellationToken ct = default)
        {
            var entity = await _db.BookingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == bookingSessionId, ct);

            if (entity == null)
                throw new NotFoundException("Không tìm thấy session");

            var now = DateTime.UtcNow;
            if (entity.State != "DRAFT" || entity.ExpiresAt <= now)
                throw new ValidationException("session", "Session đã hết hạn hoặc không còn hiệu lực");

            return new BookingSessionResponse
            {
                BookingSessionId = entity.Id,
                State = entity.State,
                ShowtimeId = entity.ShowtimeId,
                Items = JsonSerializer.Deserialize<object>(entity.ItemsJson ?? @"{""seats"":[],""combos"":[]}")!,
                Pricing = JsonSerializer.Deserialize<object>(entity.PricingJson ?? @"{""subtotal"":0,""discount"":0,""fees"":0,""total"":0,""currency"":""VND""}")!,
                ExpiresAt = entity.ExpiresAt,
                Version = entity.Version
            };
        }

        public async Task<BookingSessionResponse> TouchAsync(Guid bookingSessionId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var entity = await _db.BookingSessions
                .FirstOrDefaultAsync(x => x.Id == bookingSessionId, ct);

            if (entity == null)
                throw new NotFoundException("Không tìm thấy session");

            if (entity.State != "DRAFT")
                throw new ValidationException("session", "Session không còn trạng thái DRAFT");

            if (entity.ExpiresAt <= now)
                throw new ValidationException("session", "Session đã hết hạn");

            // 1) Gia hạn session TTL
            entity.ExpiresAt = now.Add(SessionTtl);
            entity.UpdatedAt = now;

            // 2) Gia hạn seat locks thuộc session (nếu còn hiệu lực)
            var locks = await _db.SeatLocks
                .Where(l => l.LockedBySession == bookingSessionId && l.LockedUntil > now)
                .ToListAsync(ct);

            foreach (var l in locks)
            {
                l.LockedUntil = now.Add(SeatLockTtl);
            }

            await _db.SaveChangesAsync(ct);

            return new BookingSessionResponse
            {
                BookingSessionId = entity.Id,
                State = entity.State,
                ShowtimeId = entity.ShowtimeId,
                Items = JsonSerializer.Deserialize<object>(entity.ItemsJson ?? @"{""seats"":[],""combos"":[]}")!,
                Pricing = JsonSerializer.Deserialize<object>(entity.PricingJson ?? @"{""subtotal"":0,""discount"":0,""fees"":0,""total"":0,""currency"":""VND""}")!,
                ExpiresAt = entity.ExpiresAt,
                Version = entity.Version
            };
        }
        public async Task<CancelBookingSessionResponse> CancelAsync(Guid bookingSessionId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions
                .FirstOrDefaultAsync(x => x.Id == bookingSessionId, ct);

            if (session == null)
                throw new NotFoundException("Không tìm thấy session");

            if (session.State != "DRAFT")
                throw new ValidationException("session", "Chỉ hủy được session ở trạng thái DRAFT");

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var locks = await _db.SeatLocks
                    .Where(l => l.LockedBySession == bookingSessionId)
                    .Select(l => new { l.SeatId, l.ShowtimeId })
                    .ToListAsync(ct);

                if (locks.Count > 0)
                {
                    var lockEntities = await _db.SeatLocks
                        .Where(l => l.LockedBySession == bookingSessionId)
                        .ToListAsync(ct);
                    _db.SeatLocks.RemoveRange(lockEntities);
                }

                session.State = "CANCELED";
                session.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                foreach (var lk in locks)
                {
                    await _eventStream.PublishAsync(new InfraRT.SeatEvent
                    {
                        ShowtimeId = lk.ShowtimeId,
                        SeatId = lk.SeatId,
                        Type = InfraRT.SeatEventType.Released
                    }, ct);
                }

                return new CancelBookingSessionResponse
                {
                    BookingSessionId = session.Id,
                    ShowtimeId = session.ShowtimeId,
                    ReleasedSeatIds = locks.Select(x => x.SeatId).ToList(),
                    State = session.State
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
