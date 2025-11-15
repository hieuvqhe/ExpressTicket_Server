// Application/Services/BookingSessionComboService.cs
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IBookingSessionComboService
    {
        Task<UpsertSessionCombosResponse> UpsertCombosAsync(Guid sessionId, UpsertSessionCombosRequest request, CancellationToken ct = default);
        Task<PricingPreviewResponse> PreviewPricingAsync(Guid sessionId, ClaimsPrincipal? user, PricingPreviewRequest request, CancellationToken ct = default);
    }

    public class BookingSessionComboService : IBookingSessionComboService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly IVoucherService _voucher;

        private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

        public BookingSessionComboService(CinemaDbCoreContext db, IVoucherService voucher)
        {
            _db = db;
            _voucher = voucher;
        }

        // ===== Helpers to read/write items_json =====
        private sealed class ItemsDoc
        {
            public List<int> seats { get; set; } = new();     // seat ids
            public List<int> combos { get; set; } = new();    // service ids (repeated by quantity)
        }

        private static ItemsDoc ReadItems(string? json)
        {
            try
            {
                return string.IsNullOrWhiteSpace(json)
                    ? new ItemsDoc()
                    : (JsonSerializer.Deserialize<ItemsDoc>(json!) ?? new ItemsDoc());
            }
            catch { return new ItemsDoc(); }
        }

        private static string WriteItems(ItemsDoc doc) => JsonSerializer.Serialize(doc);

        public async Task<UpsertSessionCombosResponse> UpsertCombosAsync(Guid sessionId, UpsertSessionCombosRequest request, CancellationToken ct = default)
        {
            if (request.Items == null)
                throw new ValidationException("items", "Danh sách combo không được rỗng");

            if (request.Items.Any(x => x.Quantity < 0))
                throw new ValidationException("quantity", "Quantity phải >= 0");

            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            // Show/Partner
            var show = await _db.Showtimes.Include(s => s.Cinema)
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(s => s.ShowtimeId == session.ShowtimeId, ct);
            if (show == null) throw new NotFoundException("Không tìm thấy suất chiếu");
            var partnerId = show.Cinema.PartnerId;

            // Validate services
            var ids = request.Items.Select(i => i.ServiceId).ToHashSet();
            var dbSvcs = await _db.Services.AsNoTracking()
                                .Where(s => ids.Contains(s.ServiceId))
                                .ToListAsync(ct);

            if (dbSvcs.Count != ids.Count)
                throw new ValidationException("serviceId", "Tồn tại combo không hợp lệ");

            if (dbSvcs.Any(s => s.PartnerId != partnerId))
                throw new ValidationException("serviceId", "Combo không thuộc partner của suất chiếu");

            if (dbSvcs.Any(s => !s.IsAvailable))
                throw new ValidationException("serviceId", "Có combo đang tạm ngưng bán");

            // Build flattened array for items_json.combos (repeat serviceId by quantity)
            var flattened = new List<int>();
            foreach (var it in request.Items)
            {
                if (it.Quantity == 0) continue;
                for (int i = 0; i < it.Quantity; i++) flattened.Add(it.ServiceId);
            }

            if (flattened.Count > 8)
                throw new ValidationException("quantity", "Bạn chỉ có thể chọn tối đa 8 sản phẩm combo.");

            // Save
            var doc = ReadItems(session.ItemsJson);
            doc.combos = flattened;
            session.ItemsJson = WriteItems(doc);
            session.ExpiresAt = now.Add(SessionTtl);
            session.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            // Build response (grouped)
            var grouped = flattened.GroupBy(x => x)
                                   .Select(g =>
                                   {
                                       var s = dbSvcs.First(z => z.ServiceId == g.Key);
                                       return new UpsertSessionCombosResponse.ComboQtyItem
                                       {
                                           ServiceId = s.ServiceId,
                                           Name = s.ServiceName,
                                           Code = s.Code,
                                           Price = s.Price,
                                           Quantity = g.Count(),
                                           ImageUrl = s.ImageUrl,
                                           IsAvailable = s.IsAvailable
                                       };
                                   }).OrderBy(x => x.ServiceId).ToList();

            return new UpsertSessionCombosResponse
            {
                BookingSessionId = session.Id,
                ShowtimeId = session.ShowtimeId,
                Combos = grouped,
                TotalQuantity = flattened.Count
            };
        }

        public async Task<PricingPreviewResponse> PreviewPricingAsync(Guid sessionId, ClaimsPrincipal? user, PricingPreviewRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var session = await _db.BookingSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
            if (session == null) throw new NotFoundException("Không tìm thấy session");
            if (session.State != "DRAFT") throw new ValidationException("session", "Session không còn trạng thái DRAFT");
            if (session.ExpiresAt <= now) throw new ValidationException("session", "Session đã hết hạn");

            var show = await _db.Showtimes.Include(s => s.Screen)
                                          .Include(s => s.Cinema)
                                          .FirstOrDefaultAsync(s => s.ShowtimeId == session.ShowtimeId, ct);
            if (show == null) throw new NotFoundException("Không tìm thấy suất chiếu");

            var doc = ReadItems(session.ItemsJson);

            // ===== Seats subtotal: base_price + seatType surcharge
            decimal seatsSubtotal = 0;
            if (doc.seats.Count > 0)
            {
                var seatInfo = await _db.Seats.AsNoTracking()
                    .Where(se => doc.seats.Contains(se.SeatId))
                    .Select(se => new { se.SeatId, se.SeatTypeId })
                    .ToListAsync(ct);

                var typeMap = await _db.SeatTypes.AsNoTracking()
                    .Where(st => seatInfo.Select(x => x.SeatTypeId ?? 0).Contains(st.Id))
                    .ToDictionaryAsync(st => st.Id, st => st.Surcharge, ct);

                foreach (var s in seatInfo)
                {
                    var surcharge = (s.SeatTypeId.HasValue && typeMap.TryGetValue(s.SeatTypeId.Value, out var sc)) ? sc : 0m;
                    seatsSubtotal += show.BasePrice  + surcharge;
                }
            }

            // ===== Combos subtotal: sum(price * qty)
            decimal combosSubtotal = 0;
            int comboCount = 0;
            if (doc.combos.Count > 0)
            {
                var priceMap = await _db.Services.AsNoTracking()
                    .Where(s => doc.combos.Contains(s.ServiceId))
                    .ToDictionaryAsync(s => s.ServiceId, s => s.Price, ct);

                foreach (var id in doc.combos)
                {
                    if (priceMap.TryGetValue(id, out var p)) { combosSubtotal += p; comboCount++; }
                }
            }

            var subtotal = seatsSubtotal + combosSubtotal;

            // ===== Voucher (optional)
            string? appliedCode = null;
            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(request.VoucherCode) && user?.Identity?.IsAuthenticated == true)
            {
                var validation = await _voucher.ValidateVoucherForUserAsync(request.VoucherCode.Trim().ToUpper(), subtotal);
                if (!validation.IsValid)
                {
                    throw new ValidationException("voucherCode", validation.Message);
                }
                appliedCode = validation.Voucher!.VoucherCode;
                discount = validation.DiscountAmount;
            }

            var total = Math.Max(0, subtotal - discount);

            // ===== Lưu PricingJson vào database để checkout có thể dùng
            var pricing = new PricingBreakdown
            {
                SeatsSubtotal = seatsSubtotal,
                CombosSubtotal = combosSubtotal,
                SurchargeSubtotal = 0, // Không có surcharge riêng
                Fees = 0, // Không có phí riêng
                Discount = discount,
                Total = total,
                Currency = "VND"
            };

            session.PricingJson = JsonSerializer.Serialize(pricing);
            session.CouponCode = appliedCode; // Lưu voucher code nếu có
            session.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            return new PricingPreviewResponse
            {
                BookingSessionId = session.Id,
                ShowtimeId = session.ShowtimeId,
                SeatCount = doc.seats.Count,
                ComboCount = comboCount,
                SeatsSubtotal = seatsSubtotal,
                CombosSubtotal = combosSubtotal,
                AppliedVoucherCode = appliedCode,
                DiscountAmount = discount,
                Total = total,
                Currency = "VND"
            };
        }
    }
}
