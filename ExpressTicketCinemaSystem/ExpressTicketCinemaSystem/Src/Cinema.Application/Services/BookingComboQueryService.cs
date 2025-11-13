// Application/Services/BookingComboQueryService.cs
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IBookingComboQueryService
    {
        Task<GetSessionCombosResponse> GetSessionCombosAsync(Guid bookingSessionId, ClaimsPrincipal? user, CancellationToken ct = default);
    }

    public class BookingComboQueryService : IBookingComboQueryService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly IVoucherService _voucherService;

        public BookingComboQueryService(CinemaDbCoreContext db, IVoucherService voucherService)
        {
            _db = db;
            _voucherService = voucherService;
        }

        private static bool IsAuthenticated(ClaimsPrincipal? user)
            => user?.Identity?.IsAuthenticated == true;

        private static decimal ApplyDiscountOnce(decimal basePrice, UserVoucherResponse voucher)
        {
            if (basePrice <= 0) return 0;
            decimal discount = 0;
            if (voucher.DiscountType == "fixed") discount = voucher.DiscountVal;
            else if (voucher.DiscountType == "percent") discount = basePrice * (voucher.DiscountVal / 100m);

            var after = basePrice - discount;
            return after < 0 ? 0 : after;
        }

        private static UserVoucherResponse? PickBestVoucherForPrice(decimal price, IEnumerable<UserVoucherResponse> vouchers)
        {
            UserVoucherResponse? best = null;
            decimal lowest = price;

            foreach (var v in vouchers)
            {
                var after = ApplyDiscountOnce(price, v);
                if (after < lowest)
                {
                    lowest = after;
                    best = v;
                }
            }
            return best;
        }

        public async Task<GetSessionCombosResponse> GetSessionCombosAsync(Guid bookingSessionId, ClaimsPrincipal? user, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // 1) Session phải tồn tại + còn DRAFT + chưa hết hạn
            var session = await _db.BookingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == bookingSessionId, ct);

            if (session == null)
                throw new NotFoundException("Không tìm thấy session");

            if (session.State != "DRAFT" || session.ExpiresAt <= now)
                throw new ValidationException("session", "Session đã hết hạn hoặc không còn hiệu lực");

            // 2) Xác định partner của suất chiếu
            var showtime = await _db.Showtimes
                .Include(s => s.Cinema)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == session.ShowtimeId, ct);

            if (showtime == null)
                throw new NotFoundException("Không tìm thấy suất chiếu");

            var partnerId = showtime.Cinema.PartnerId;

            // 3) Lấy combos (Service) khả dụng của partner
            var services = await _db.Services
                .AsNoTracking()
                .Where(s => s.PartnerId == partnerId && s.IsAvailable)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);

            var resp = new GetSessionCombosResponse
            {
                BookingSessionId = bookingSessionId,
                ShowtimeId = session.ShowtimeId,
                PartnerId = partnerId
            };

            // 4) Nếu user đăng nhập → lấy list vouchers hợp lệ & chọn voucher tốt nhất cho từng combo (áp 1 đơn vị)
            List<UserVoucherResponse>? vouchers = null;
            UserVoucherResponse? autoVoucher = null;

            if (IsAuthenticated(user))
            {
                vouchers = await _voucherService.GetValidVouchersForUserAsync();

                // Chọn 1 auto voucher "tốt nhất" theo mức giảm trên combo có giá cao nhất (heuristic dễ hiểu)
                var topPrice = services.Count > 0 ? services.Max(s => s.Price) : 0m;
                autoVoucher = (topPrice > 0 && vouchers.Count > 0)
                    ? PickBestVoucherForPrice(topPrice, vouchers)
                    : null;
            }

            // 5) Map danh sách combo
            foreach (var s in services)
            {
                var item = new SessionComboItem
                {
                    ServiceId = s.ServiceId,
                    Name = s.ServiceName,
                    Code = s.Code,
                    Price = s.Price,
                    ImageUrl = s.ImageUrl,
                    Description = s.Description,
                    IsAvailable = s.IsAvailable
                };

                if (autoVoucher != null)
                {
                    item.PriceAfterAutoDiscount = ApplyDiscountOnce(s.Price, autoVoucher);
                    item.AutoVoucherCode = autoVoucher.VoucherCode;
                }

                resp.Combos.Add(item);
            }

            if (vouchers != null)
            {
                resp.Vouchers = vouchers;
                resp.AutoVoucher = autoVoucher;
            }
            resp.ServerTime = DateTime.UtcNow;
            resp.Currency = "VND";
            resp.SelectionLimit = 8;
            return resp;
        }
    }
}
