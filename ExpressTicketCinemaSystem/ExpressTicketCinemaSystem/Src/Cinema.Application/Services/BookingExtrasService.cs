// Application/Services/BookingExtrasService.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IBookingExtrasService
    {
        Task<UpdateCombosResponse> ReplaceCombosAsync(Guid sessionId, UpdateCombosRequest req, CancellationToken ct = default);
        Task<RemoveComboResponse> RemoveComboAsync(Guid sessionId, int serviceId, CancellationToken ct = default);
        Task<SetVoucherResponse> SetVoucherAsync(Guid sessionId, string voucherCode, CancellationToken ct = default);
        Task<RemoveVoucherResponse> RemoveVoucherAsync(Guid sessionId, CancellationToken ct = default);
    }

    public class BookingExtrasService : IBookingExtrasService
    {
        private readonly CinemaDbCoreContext _db;
        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

        public BookingExtrasService(CinemaDbCoreContext db)
        {
            _db = db;
        }

        // ===== Helpers: ItemsJson { seats: number[], combos: number[] }
        private static (List<int> seats, List<int> combos, string? voucher) ReadItems(string? itemsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemsJson))
                    return (new(), new(), null);

                using var doc = JsonDocument.Parse(itemsJson);
                var root = doc.RootElement;

                var seats = root.TryGetProperty("seats", out var sEl) && sEl.ValueKind == JsonValueKind.Array
                    ? sEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                var combos = root.TryGetProperty("combos", out var cEl) && cEl.ValueKind == JsonValueKind.Array
                    ? cEl.EnumerateArray().Select(x => x.GetInt32()).ToList()
                    : new List<int>();

                string? voucher = null;
                if (root.TryGetProperty("voucher", out var vEl) && vEl.ValueKind == JsonValueKind.String)
                    voucher = vEl.GetString();

                return (seats, combos, voucher);
            }
            catch
            {
                return (new(), new(), null);
            }
        }

        private static string WriteItems(List<int> seats, List<int> combos, string? voucher)
            => JsonSerializer.Serialize(new { seats, combos, voucher });

        private async Task<(Infrastructure.Models.BookingSession session, int showtimeId)> LoadAliveSession(Guid sessionId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            return (session, session.ShowtimeId);
        }

        // ===== Combos: PUT (replace all)
        public async Task<UpdateCombosResponse> ReplaceCombosAsync(Guid sessionId, UpdateCombosRequest req, CancellationToken ct = default)
        {
            if (req.Items == null) throw new ValidationException("items", "Danh sách combo không được rỗng");

            // flatten thành list<int> theo quantity
            var flat = new List<int>();
            foreach (var i in req.Items)
            {
                if (i.ServiceId <= 0) throw new ValidationException("serviceId", "ServiceId không hợp lệ");
                if (i.Quantity < 0) throw new ValidationException("quantity", "Quantity phải >= 0");

                for (int k = 0; k < i.Quantity; k++) flat.Add(i.ServiceId);
            }

            if (flat.Count > 8)
                throw new ValidationException("items", "Tổng combo tối đa 8 đơn vị");

            var (session, showtimeId) = await LoadAliveSession(sessionId, ct);

            // validate service thuộc partner của showtime & còn IsAvailable
            var st = await _db.Showtimes.Include(s => s.Cinema).FirstAsync(x => x.ShowtimeId == showtimeId, ct);
            var partnerId = st.Cinema.PartnerId;

            if (flat.Count > 0)
            {
                var svcIds = flat.Distinct().ToList();
                var validIds = await _db.Services
                    .Where(s => s.PartnerId == partnerId && s.IsAvailable && svcIds.Contains(s.ServiceId))
                    .Select(s => s.ServiceId)
                    .ToListAsync(ct);

                var invalid = svcIds.Except(validIds).ToList();
                if (invalid.Count > 0)
                    throw new ValidationException("serviceId", $"Combo không hợp lệ/không khả dụng: {string.Join(",", invalid)}");
            }

            // save
            var (curSeats, _, voucher) = ReadItems(session.ItemsJson);
            session.ItemsJson = WriteItems(curSeats, flat, voucher);
            session.ExpiresAt = DateTime.UtcNow.Add(SessionTtl);
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new UpdateCombosResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                TotalUnits = flat.Count,
                ComboIds = flat
            };
        }

        // ===== Combos: DELETE 1 loại (remove all units of that serviceId)
        public async Task<RemoveComboResponse> RemoveComboAsync(Guid sessionId, int serviceId, CancellationToken ct = default)
        {
            if (serviceId <= 0) throw new ValidationException("serviceId", "ServiceId không hợp lệ");

            var (session, showtimeId) = await LoadAliveSession(sessionId, ct);

            var (seats, combos, voucher) = ReadItems(session.ItemsJson);
            if (combos.Count == 0)
                return new RemoveComboResponse
                {
                    BookingSessionId = sessionId,
                    ShowtimeId = showtimeId,
                    RemovedServiceId = serviceId,
                    TotalUnits = 0,
                    ComboIds = combos
                };

            combos = combos.Where(id => id != serviceId).ToList();

            session.ItemsJson = WriteItems(seats, combos, voucher);
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new RemoveComboResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                RemovedServiceId = serviceId,
                TotalUnits = combos.Count,
                ComboIds = combos
            };
        }

        // ===== Voucher: PUT (set/replace) — chỉ lưu code, tính tiền ở /pricing/preview
        public async Task<SetVoucherResponse> SetVoucherAsync(Guid sessionId, string voucherCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
                throw new ValidationException("voucherCode", "VoucherCode là bắt buộc");

            var (session, showtimeId) = await LoadAliveSession(sessionId, ct);

            var (seats, combos, _) = ReadItems(session.ItemsJson);
            session.ItemsJson = WriteItems(seats, combos, voucherCode.Trim().ToUpperInvariant());
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new SetVoucherResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId,
                VoucherCode = voucherCode.Trim().ToUpperInvariant()
            };
        }

        // ===== Voucher: DELETE (remove)
        public async Task<RemoveVoucherResponse> RemoveVoucherAsync(Guid sessionId, CancellationToken ct = default)
        {
            var (session, showtimeId) = await LoadAliveSession(sessionId, ct);

            var (seats, combos, _) = ReadItems(session.ItemsJson);
            session.ItemsJson = WriteItems(seats, combos, null);
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new RemoveVoucherResponse
            {
                BookingSessionId = sessionId,
                ShowtimeId = showtimeId
            };
        }
    }
}
