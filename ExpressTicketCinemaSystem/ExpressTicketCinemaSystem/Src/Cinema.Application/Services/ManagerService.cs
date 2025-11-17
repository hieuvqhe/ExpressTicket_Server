using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ManagerService : IManagerService
    {
        private readonly CinemaDbCoreContext _context;

        public ManagerService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<int> GetManagerIdByUserIdAsync(int userId)
        {
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null)
            {
                throw new UnauthorizedException("Người dùng không phải là manager.");
            }

            return manager.ManagerId;
        }

        public async Task<int> GetDefaultManagerIdAsync()
        {
            var manager = await _context.Managers
                .OrderBy(m => m.ManagerId)
                .FirstOrDefaultAsync();

            if (manager == null)
            {
                throw new InvalidOperationException("Không tìm thấy manager trong hệ thống.");
            }

            return manager.ManagerId;
        }

        public async Task<bool> ValidateManagerExistsAsync(int managerId)
        {
            return await _context.Managers
                .AnyAsync(m => m.ManagerId == managerId);
        }

        public async Task<bool> IsUserManagerAsync(int userId)
        {
            return await _context.Managers
                .AnyAsync(m => m.UserId == userId);
        }

        /// <summary>
        /// Get all bookings (Manager only) with filtering and pagination
        /// Manager can see bookings from all partners and all cinemas
        /// </summary>
        public async Task<ManagerBookingsResponse> GetManagerBookingsAsync(int userId, GetManagerBookingsRequest request)
        {
            // Validate manager user
            var manager = await _context.Managers
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null || manager.User == null || !manager.User.IsActive || manager.User.UserType.ToLower() != "manager")
                throw new UnauthorizedException("Bạn không có quyền truy cập. Chỉ Manager mới có thể xem tất cả đơn hàng.");

            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Validate sort parameters
            var validSortBy = new[] { "booking_time", "total_amount", "created_at", "customer_name", "partner_name", "cinema_name" };
            if (!validSortBy.Contains(request.SortBy.ToLower()))
                throw new ValidationException("sortBy", "SortBy phải là một trong: booking_time, total_amount, created_at, customer_name, partner_name, cinema_name.");

            var validSortOrder = new[] { "asc", "desc" };
            if (!validSortOrder.Contains(request.SortOrder.ToLower()))
                throw new ValidationException("sortOrder", "SortOrder phải là asc hoặc desc.");

            // Validate amount range
            if (request.MinAmount.HasValue && request.MaxAmount.HasValue)
            {
                if (request.MaxAmount.Value < request.MinAmount.Value)
                    throw new ValidationException("maxAmount", "MaxAmount phải lớn hơn hoặc bằng MinAmount.");
            }

            // Validate date range
            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                if (request.ToDate.Value < request.FromDate.Value)
                    throw new ValidationException("toDate", "ToDate phải lớn hơn hoặc bằng FromDate.");
            }

            // Build query - Manager: NO LIMIT, can see all bookings
            var query = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                        .ThenInclude(c => c.Partner)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                .AsNoTracking();

            // Apply filters
            if (request.PartnerId.HasValue)
            {
                query = query.Where(b => b.Showtime.Cinema.PartnerId == request.PartnerId.Value);
            }

            if (request.CinemaId.HasValue)
            {
                query = query.Where(b => b.Showtime.CinemaId == request.CinemaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusLower = request.Status.Trim().ToLower();
                query = query.Where(b =>
                    b.Status.ToLower() == statusLower ||
                    b.State.ToLower() == statusLower);
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                var paymentStatusLower = request.PaymentStatus.Trim().ToLower();
                query = query.Where(b => b.PaymentStatus != null && b.PaymentStatus.ToLower() == paymentStatusLower);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(b => b.BookingTime >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                var toDateEndOfDay = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(b => b.BookingTime < toDateEndOfDay);
            }

            if (request.CustomerId.HasValue)
            {
                query = query.Where(b => b.CustomerId == request.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                var emailSearch = request.CustomerEmail.Trim().ToLower();
                query = query.Where(b => b.Customer.User.Email != null && b.Customer.User.Email.ToLower().Contains(emailSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                var phoneSearch = request.CustomerPhone.Trim();
                query = query.Where(b => b.Customer.User.Phone != null && b.Customer.User.Phone.Contains(phoneSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerName))
            {
                var nameSearch = request.CustomerName.Trim().ToLower();
                query = query.Where(b => b.Customer.User.Fullname != null && b.Customer.User.Fullname.ToLower().Contains(nameSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.BookingCode))
            {
                var bookingCodeSearch = request.BookingCode.Trim().ToUpper();
                query = query.Where(b => b.BookingCode.ToUpper().Contains(bookingCodeSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var orderCodeSearch = request.OrderCode.Trim().ToUpper();
                query = query.Where(b => b.OrderCode != null && b.OrderCode.ToUpper().Contains(orderCodeSearch));
            }

            if (request.MovieId.HasValue)
            {
                query = query.Where(b => b.Showtime.MovieId == request.MovieId.Value);
            }

            if (request.MinAmount.HasValue)
            {
                query = query.Where(b => b.TotalAmount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                query = query.Where(b => b.TotalAmount <= request.MaxAmount.Value);
            }

            // Get total count
            var totalItems = await query.CountAsync();

            // Apply sorting
            var sortBy = request.SortBy.ToLower();
            var isAscending = request.SortOrder.ToLower() == "asc";

            query = sortBy switch
            {
                "total_amount" => isAscending ? query.OrderBy(b => b.TotalAmount) : query.OrderByDescending(b => b.TotalAmount),
                "created_at" => isAscending ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                "customer_name" => isAscending 
                    ? query.OrderBy(b => b.Customer.User.Fullname ?? "") 
                    : query.OrderByDescending(b => b.Customer.User.Fullname ?? ""),
                "partner_name" => isAscending 
                    ? query.OrderBy(b => b.Showtime.Cinema.Partner.PartnerName) 
                    : query.OrderByDescending(b => b.Showtime.Cinema.Partner.PartnerName),
                "cinema_name" => isAscending 
                    ? query.OrderBy(b => b.Showtime.Cinema.CinemaName ?? "") 
                    : query.OrderByDescending(b => b.Showtime.Cinema.CinemaName ?? ""),
                _ => isAscending ? query.OrderBy(b => b.BookingTime) : query.OrderByDescending(b => b.BookingTime) // default booking_time
            };

            // Apply pagination
            var bookings = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to response DTO
            var items = bookings.Select(b => new ManagerBookingItemDto
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                BookingTime = b.BookingTime,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                State = b.State,
                PaymentStatus = b.PaymentStatus,
                PaymentProvider = b.PaymentProvider,
                PaymentTxId = b.PaymentTxId,
                OrderCode = b.OrderCode,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                TicketCount = b.Tickets.Count,
                Customer = new ManagerBookingCustomerDto
                {
                    CustomerId = b.Customer.CustomerId,
                    UserId = b.Customer.UserId,
                    Fullname = b.Customer.User.Fullname,
                    Email = b.Customer.User.Email,
                    Phone = b.Customer.User.Phone
                },
                Showtime = new ManagerBookingShowtimeDto
                {
                    ShowtimeId = b.Showtime.ShowtimeId,
                    ShowDatetime = b.Showtime.ShowDatetime,
                    EndTime = b.Showtime.EndTime,
                    FormatType = b.Showtime.FormatType,
                    Status = b.Showtime.Status
                },
                Cinema = new ManagerBookingCinemaDto
                {
                    CinemaId = b.Showtime.Cinema.CinemaId,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    Address = b.Showtime.Cinema.Address,
                    City = b.Showtime.Cinema.City,
                    District = b.Showtime.Cinema.District
                },
                Partner = new ManagerBookingPartnerDto
                {
                    PartnerId = b.Showtime.Cinema.Partner.PartnerId,
                    PartnerName = b.Showtime.Cinema.Partner.PartnerName,
                    TaxCode = b.Showtime.Cinema.Partner.TaxCode
                },
                Movie = new ManagerBookingMovieDto
                {
                    MovieId = b.Showtime.Movie.MovieId,
                    Title = b.Showtime.Movie.Title,
                    DurationMinutes = b.Showtime.Movie.DurationMinutes,
                    PosterUrl = b.Showtime.Movie.PosterUrl,
                    Genre = b.Showtime.Movie.Genre
                }
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

            return new ManagerBookingsResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }

        /// <summary>
        /// Get detailed information of a specific booking (Manager only)
        /// Manager can see booking details from all partners and all cinemas
        /// </summary>
        public async Task<BookingDetailResponse> GetBookingDetailAsync(int userId, int bookingId)
        {
            // Validate manager user
            var manager = await _context.Managers
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null || manager.User == null || !manager.User.IsActive || manager.User.UserType.ToLower() != "manager")
                throw new UnauthorizedException("Bạn không có quyền truy cập. Chỉ Manager mới có thể xem chi tiết đơn hàng.");

            // Get booking with all related data
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                        .ThenInclude(c => c.Partner)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Screen)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(b => b.ServiceOrders)
                    .ThenInclude(so => so.Service)
                .Include(b => b.Payment)
                .Include(b => b.Voucher)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                throw new NotFoundException($"Không tìm thấy đơn hàng với ID {bookingId}.");

            // Calculate pricing breakdown
            decimal ticketsSubtotal = booking.Tickets.Sum(t => t.Price);
            decimal servicesSubtotal = booking.ServiceOrders.Sum(so => so.Quantity * so.UnitPrice);
            decimal subtotalBeforeVoucher = ticketsSubtotal + servicesSubtotal;
            decimal voucherDiscount = 0;

            if (booking.Voucher != null)
            {
                if (booking.Voucher.DiscountType.ToLower() == "fixed")
                {
                    voucherDiscount = booking.Voucher.DiscountVal;
                }
                else if (booking.Voucher.DiscountType.ToLower() == "percent")
                {
                    voucherDiscount = subtotalBeforeVoucher * (booking.Voucher.DiscountVal / 100m);
                }
            }

            decimal finalTotal = booking.TotalAmount;

            // Calculate commission
            decimal? commissionAmount = null;
            decimal? commissionRate = booking.Showtime.Cinema.Partner.CommissionRate;
            
            if (commissionRate.HasValue && commissionRate.Value > 0)
            {
                commissionAmount = finalTotal * (commissionRate.Value / 100m);
            }

            // Build response
            var response = new BookingDetailResponse
            {
                Booking = new BookingInfoDto
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    BookingTime = booking.BookingTime,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    State = booking.State,
                    PaymentProvider = booking.PaymentProvider,
                    PaymentTxId = booking.PaymentTxId,
                    PaymentStatus = booking.PaymentStatus,
                    OrderCode = booking.OrderCode,
                    SessionId = booking.SessionId,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                },
                Customer = new BookingDetailCustomerDto
                {
                    CustomerId = booking.Customer.CustomerId,
                    UserId = booking.Customer.UserId,
                    Fullname = booking.Customer.User.Fullname,
                    Email = booking.Customer.User.Email,
                    Phone = booking.Customer.User.Phone,
                    Username = booking.Customer.User.Username
                },
                Showtime = new BookingDetailShowtimeDto
                {
                    ShowtimeId = booking.Showtime.ShowtimeId,
                    ShowDatetime = booking.Showtime.ShowDatetime,
                    EndTime = booking.Showtime.EndTime,
                    BasePrice = booking.Showtime.BasePrice,
                    Status = booking.Showtime.Status,
                    FormatType = booking.Showtime.FormatType,
                    AvailableSeats = booking.Showtime.AvailableSeats
                },
                Cinema = new BookingDetailCinemaDto
                {
                    CinemaId = booking.Showtime.Cinema.CinemaId,
                    CinemaName = booking.Showtime.Cinema.CinemaName,
                    Address = booking.Showtime.Cinema.Address,
                    City = booking.Showtime.Cinema.City,
                    District = booking.Showtime.Cinema.District,
                    Code = booking.Showtime.Cinema.Code,
                    IsActive = booking.Showtime.Cinema.IsActive ?? false
                },
                Screen = new BookingDetailScreenDto
                {
                    ScreenId = booking.Showtime.Screen.ScreenId,
                    ScreenName = booking.Showtime.Screen.ScreenName,
                    Code = booking.Showtime.Screen.Code,
                    Capacity = booking.Showtime.Screen.Capacity,
                    ScreenType = booking.Showtime.Screen.ScreenType,
                    SoundSystem = booking.Showtime.Screen.SoundSystem
                },
                Partner = new BookingDetailPartnerDto
                {
                    PartnerId = booking.Showtime.Cinema.Partner.PartnerId,
                    PartnerName = booking.Showtime.Cinema.Partner.PartnerName,
                    TaxCode = booking.Showtime.Cinema.Partner.TaxCode,
                    Status = booking.Showtime.Cinema.Partner.Status,
                    CommissionRate = booking.Showtime.Cinema.Partner.CommissionRate
                },
                Movie = new BookingDetailMovieDto
                {
                    MovieId = booking.Showtime.Movie.MovieId,
                    Title = booking.Showtime.Movie.Title,
                    Genre = booking.Showtime.Movie.Genre,
                    DurationMinutes = booking.Showtime.Movie.DurationMinutes,
                    Director = booking.Showtime.Movie.Director,
                    Language = booking.Showtime.Movie.Language,
                    Country = booking.Showtime.Movie.Country,
                    PosterUrl = booking.Showtime.Movie.PosterUrl,
                    PremiereDate = booking.Showtime.Movie.PremiereDate,
                    EndDate = booking.Showtime.Movie.EndDate
                },
                Tickets = booking.Tickets.Select(t => new BookingTicketDto
                {
                    TicketId = t.TicketId,
                    SeatId = t.SeatId,
                    SeatName = t.Seat.SeatName,
                    RowCode = t.Seat.RowCode,
                    SeatNumber = t.Seat.SeatNumber,
                    SeatTypeName = t.Seat.SeatType?.Name,
                    Price = t.Price,
                    Status = t.Status
                }).ToList(),
                ServiceOrders = booking.ServiceOrders.Select(so => new BookingServiceOrderDto
                {
                    OrderId = so.OrderId,
                    ServiceId = so.ServiceId,
                    ServiceName = so.Service.ServiceName,
                    Description = so.Service.Description,
                    Quantity = so.Quantity,
                    UnitPrice = so.UnitPrice,
                    TotalPrice = so.Quantity * so.UnitPrice
                }).ToList(),
                Payment = booking.Payment == null ? null : new BookingPaymentDto
                {
                    PaymentId = booking.Payment.PaymentId,
                    Amount = booking.Payment.Amount,
                    Method = booking.Payment.Method,
                    Status = booking.Payment.Status,
                    Provider = booking.Payment.Provider,
                    TransactionId = booking.Payment.TransactionId,
                    PaidAt = booking.Payment.PaidAt,
                    SignatureOk = booking.Payment.SignatureOk
                },
                Voucher = booking.Voucher == null ? null : new BookingVoucherDto
                {
                    VoucherId = booking.Voucher.VoucherId,
                    VoucherCode = booking.Voucher.VoucherCode,
                    DiscountType = booking.Voucher.DiscountType,
                    DiscountVal = booking.Voucher.DiscountVal,
                    ValidFrom = booking.Voucher.ValidFrom.ToDateTime(TimeOnly.MinValue),
                    ValidTo = booking.Voucher.ValidTo.ToDateTime(TimeOnly.MinValue)
                },
                PricingBreakdown = new PricingBreakdownDto
                {
                    TicketsSubtotal = ticketsSubtotal,
                    ServicesSubtotal = servicesSubtotal,
                    SubtotalBeforeVoucher = subtotalBeforeVoucher,
                    VoucherDiscount = voucherDiscount,
                    FinalTotal = finalTotal,
                    CommissionAmount = commissionAmount,
                    CommissionRate = commissionRate
                }
            };

            return response;
        }
    }
}