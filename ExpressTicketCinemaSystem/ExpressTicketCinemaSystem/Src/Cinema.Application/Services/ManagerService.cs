using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using System.Linq;

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

        /// <summary>
        /// Get booking statistics (Manager only)
        /// Manager can see statistics from all partners and all cinemas
        /// </summary>
        public async Task<ManagerBookingStatisticsResponse> GetBookingStatisticsAsync(int userId, GetManagerBookingStatisticsRequest request)
        {
            // Validate manager user
            var manager = await _context.Managers
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null || manager.User == null || !manager.User.IsActive || manager.User.UserType.ToLower() != "manager")
                throw new UnauthorizedException("Bạn không có quyền truy cập. Chỉ Manager mới có thể xem thống kê đơn hàng.");

            // Validate top limit
            if (request.TopLimit < 1 || request.TopLimit > 50)
                throw new ValidationException("topLimit", "TopLimit phải trong khoảng 1-50.");

            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Validate groupBy
            var validGroupBy = new[] { "day", "week", "month" };
            if (!validGroupBy.Contains(request.GroupBy.ToLower()))
                throw new ValidationException("groupBy", "GroupBy phải là một trong: day, week, month.");

            // Set default date range if not provided
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30).Date;
            var toDate = request.ToDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // End of day

            // Validate date range
            if (toDate < fromDate)
                throw new ValidationException("toDate", "ToDate phải lớn hơn hoặc bằng FromDate.");

            // Base query for all bookings in date range
            var baseQuery = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                        .ThenInclude(c => c.Partner)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                .Include(b => b.Voucher)
                .AsNoTracking()
                .Where(b => b.BookingTime >= fromDate && b.BookingTime <= toDate);

            // Apply filters
            if (request.PartnerId.HasValue)
                baseQuery = baseQuery.Where(b => b.Showtime.Cinema.PartnerId == request.PartnerId.Value);

            if (request.CinemaId.HasValue)
                baseQuery = baseQuery.Where(b => b.Showtime.CinemaId == request.CinemaId.Value);

            if (request.MovieId.HasValue)
                baseQuery = baseQuery.Where(b => b.Showtime.MovieId == request.MovieId.Value);

            var bookings = await baseQuery.ToListAsync();

            var response = new ManagerBookingStatisticsResponse();

            // ========== OVERVIEW STATISTICS ==========
            response.Overview = await CalculateOverviewStatisticsAsync(bookings);

            // ========== CINEMA REVENUE STATISTICS ==========
            response.CinemaRevenue = await CalculateCinemaRevenueStatisticsAsync(bookings, request.TopLimit, request.Page, request.PageSize);

            // ========== TOP CUSTOMERS STATISTICS ==========
            response.TopCustomers = await CalculateTopCustomersStatisticsAsync(bookings, request.TopLimit);

            // ========== PARTNER REVENUE STATISTICS ==========
            response.PartnerRevenue = await CalculatePartnerRevenueStatisticsAsync(bookings, request.TopLimit, request.Page, request.PageSize);

            // ========== MOVIE STATISTICS ==========
            response.MovieStatistics = await CalculateMovieStatisticsAsync(bookings, request.TopLimit);

            // ========== TIME-BASED STATISTICS ==========
            response.TimeStatistics = await CalculateTimeBasedStatisticsAsync(bookings, fromDate, toDate, request.GroupBy, request.IncludeComparison);

            // ========== VOUCHER STATISTICS ==========
            response.VoucherStatistics = CalculateVoucherStatistics(bookings);

            // ========== PAYMENT STATISTICS ==========
            response.PaymentStatistics = CalculatePaymentStatistics(bookings);

            return response;
        }

        /// <summary>
        /// Helper method to check if a booking is paid/completed
        /// A booking is considered paid if PaymentStatus is "PAID" 
        /// or if Status is "CONFIRMED" with PaymentStatus "PAID"
        /// </summary>
        private bool IsPaidBooking(Booking booking)
        {
            var paymentStatus = booking.PaymentStatus?.ToUpper() ?? "";
            var status = booking.Status.ToUpper();
            var state = booking.State.ToUpper();
            
            // Check PaymentStatus first (most reliable)
            if (paymentStatus == "PAID")
                return true;
            
            // Also check if Status is CONFIRMED and PaymentStatus is PAID
            if (status == "CONFIRMED" && paymentStatus == "PAID")
                return true;
            
            // Check if State is COMPLETED and PaymentStatus is PAID
            if (state == "COMPLETED" && paymentStatus == "PAID")
                return true;
            
            return false;
        }

        private async Task<BookingOverviewStatistics> CalculateOverviewStatisticsAsync(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var totalTickets = paidBookings.Sum(b => b.Tickets.Count);
            var totalCustomers = bookings.Select(b => b.CustomerId).Distinct().Count();
            var totalRevenue = paidBookings.Sum(b => b.TotalAmount);
            var averageOrderValue = paidBookings.Any() ? totalRevenue / paidBookings.Count : 0;

            var bookingsByStatus = bookings
                .GroupBy(b => b.Status.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());

            var revenueByStatus = paidBookings
                .GroupBy(b => b.Status.ToUpper())
                .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalAmount));

            var bookingsByPaymentStatus = bookings
                .Where(b => !string.IsNullOrEmpty(b.PaymentStatus))
                .GroupBy(b => b.PaymentStatus!.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());

            return new BookingOverviewStatistics
            {
                TotalBookings = bookings.Count,
                TotalRevenue = totalRevenue,
                TotalPaidBookings = paidBookings.Count,
                TotalPendingBookings = bookings.Count(b => b.Status.ToUpper() == "PENDING_PAYMENT" || b.State.ToUpper() == "PENDING_PAYMENT"),
                TotalCancelledBookings = bookings.Count(b => b.Status.ToUpper() == "CANCELLED" || b.State.ToUpper() == "CANCELLED"),
                TotalTicketsSold = totalTickets,
                TotalCustomers = totalCustomers,
                AverageOrderValue = averageOrderValue,
                BookingsByStatus = bookingsByStatus,
                RevenueByStatus = revenueByStatus,
                BookingsByPaymentStatus = bookingsByPaymentStatus
            };
        }

        private async Task<CinemaRevenueStatistics> CalculateCinemaRevenueStatisticsAsync(List<Booking> bookings, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var allCinemaStats = paidBookings
                .GroupBy(b => new
                {
                    CinemaId = b.Showtime.Cinema.CinemaId,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    PartnerId = b.Showtime.Cinema.Partner.PartnerId,
                    PartnerName = b.Showtime.Cinema.Partner.PartnerName,
                    City = b.Showtime.Cinema.City,
                    District = b.Showtime.Cinema.District,
                    Address = b.Showtime.Cinema.Address
                })
                .Select(g => new CinemaRevenueStat
                {
                    CinemaId = g.Key.CinemaId,
                    CinemaName = g.Key.CinemaName,
                    PartnerId = g.Key.PartnerId,
                    PartnerName = g.Key.PartnerName,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    AverageOrderValue = g.Average(b => b.TotalAmount),
                    City = g.Key.City,
                    District = g.Key.District,
                    Address = g.Key.Address
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            var totalCount = allCinemaStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Paginated list for CinemaRevenueList
            var cinemaStats = allCinemaStats
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var topCinemas = allCinemaStats.Take(topLimit).ToList();

            var comparison = new CinemaRevenueComparison
            {
                HighestRevenueCinema = allCinemaStats.FirstOrDefault(),
                LowestRevenueCinema = allCinemaStats.LastOrDefault(c => c.TotalRevenue > 0),
                AverageRevenuePerCinema = allCinemaStats.Any() ? allCinemaStats.Average(c => c.TotalRevenue) : 0
            };

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return new CinemaRevenueStatistics
            {
                CinemaRevenueList = cinemaStats,
                TopCinemasByRevenue = topCinemas,
                Comparison = allCinemaStats.Any() ? comparison : null,
                Pagination = pagination
            };
        }

        private async Task<TopCustomersStatistics> CalculateTopCustomersStatisticsAsync(List<Booking> bookings, int topLimit)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var customerStats = paidBookings
                .GroupBy(b => new
                {
                    b.CustomerId,
                    UserId = b.Customer.User.UserId,
                    Fullname = b.Customer.User.Fullname,
                    Email = b.Customer.User.Email,
                    Phone = b.Customer.User.Phone
                })
                .Select(g => new CustomerStat
                {
                    CustomerId = g.Key.CustomerId,
                    UserId = g.Key.UserId,
                    Fullname = g.Key.Fullname,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    TotalSpent = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsPurchased = g.Sum(b => b.Tickets.Count),
                    AverageOrderValue = g.Average(b => b.TotalAmount),
                    LastBookingDate = g.Max(b => b.BookingTime)
                })
                .ToList();

            var topByRevenue = customerStats
                .OrderByDescending(c => c.TotalSpent)
                .Take(topLimit)
                .ToList();

            var topByBookingCount = customerStats
                .OrderByDescending(c => c.TotalBookings)
                .Take(topLimit)
                .ToList();

            return new TopCustomersStatistics
            {
                ByRevenue = topByRevenue,
                ByBookingCount = topByBookingCount
            };
        }

        private async Task<PartnerRevenueStatistics> CalculatePartnerRevenueStatisticsAsync(List<Booking> bookings, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var allPartnerStats = paidBookings
                .GroupBy(b => new
                {
                    PartnerId = b.Showtime.Cinema.Partner.PartnerId,
                    PartnerName = b.Showtime.Cinema.Partner.PartnerName,
                    TaxCode = b.Showtime.Cinema.Partner.TaxCode
                })
                .Select(g => new PartnerRevenueStat
                {
                    PartnerId = g.Key.PartnerId,
                    PartnerName = g.Key.PartnerName,
                    TaxCode = g.Key.TaxCode,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalCinemas = g.Select(b => b.Showtime.CinemaId).Distinct().Count(),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    AverageRevenuePerCinema = g.Select(b => b.Showtime.CinemaId).Distinct().Count() > 0
                        ? g.Sum(b => b.TotalAmount) / g.Select(b => b.Showtime.CinemaId).Distinct().Count()
                        : 0
                })
                .OrderByDescending(p => p.TotalRevenue)
                .ToList();

            var totalCount = allPartnerStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Paginated list for PartnerRevenueList
            var partnerStats = allPartnerStats
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var topPartners = allPartnerStats.Take(topLimit).ToList();

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return new PartnerRevenueStatistics
            {
                PartnerRevenueList = partnerStats,
                TopPartnersByRevenue = topPartners,
                Pagination = pagination
            };
        }

        private async Task<MovieRevenueStatistics> CalculateMovieStatisticsAsync(List<Booking> bookings, int topLimit)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var movieStats = paidBookings
                .GroupBy(b => new
                {
                    MovieId = b.Showtime.Movie.MovieId,
                    Title = b.Showtime.Movie.Title,
                    Genre = b.Showtime.Movie.Genre
                })
                .Select(g => new MovieRevenueStat
                {
                    MovieId = g.Key.MovieId,
                    Title = g.Key.Title,
                    Genre = g.Key.Genre,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    ShowtimeCount = g.Select(b => b.ShowtimeId).Distinct().Count()
                })
                .ToList();

            var topByRevenue = movieStats
                .OrderByDescending(m => m.TotalRevenue)
                .Take(topLimit)
                .ToList();

            var topByTickets = movieStats
                .OrderByDescending(m => m.TotalTicketsSold)
                .Take(topLimit)
                .ToList();

            return new MovieRevenueStatistics
            {
                TopMoviesByRevenue = topByRevenue,
                TopMoviesByTickets = topByTickets
            };
        }

        private async Task<TimeBasedStatistics> CalculateTimeBasedStatisticsAsync(List<Booking> bookings, DateTime fromDate, DateTime toDate, string groupBy, bool includeComparison)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var thisYearStart = new DateTime(today.Year, 1, 1);

            var todayStats = CalculateTimePeriodStat(paidBookings, today, today.AddDays(1));
            var yesterdayStats = CalculateTimePeriodStat(paidBookings, yesterday, today);
            var thisWeekStats = CalculateTimePeriodStat(paidBookings, thisWeekStart, today.AddDays(1));
            var thisMonthStats = CalculateTimePeriodStat(paidBookings, thisMonthStart, today.AddDays(1));
            var thisYearStats = CalculateTimePeriodStat(paidBookings, thisYearStart, today.AddDays(1));

            var revenueTrend = CalculateRevenueTrend(paidBookings, fromDate, toDate, groupBy);

            PeriodComparison? periodComparison = null;
            if (includeComparison)
            {
                var periodDays = (toDate - fromDate).Days;
                var previousFromDate = fromDate.AddDays(-periodDays - 1);
                var previousToDate = fromDate.AddTicks(-1);

                var previousBookingsRaw = await _context.Bookings
                    .Include(b => b.Tickets)
                    .AsNoTracking()
                    .Where(b => b.BookingTime >= previousFromDate && b.BookingTime <= previousToDate)
                    .ToListAsync();
                
                var previousBookings = previousBookingsRaw.Where(b => IsPaidBooking(b)).ToList();

                var currentPeriod = new PeriodData
                {
                    Revenue = paidBookings.Sum(b => b.TotalAmount),
                    Bookings = paidBookings.Count,
                    Customers = paidBookings.Select(b => b.CustomerId).Distinct().Count()
                };

                var previousPeriod = new PeriodData
                {
                    Revenue = previousBookings.Sum(b => b.TotalAmount),
                    Bookings = previousBookings.Count,
                    Customers = previousBookings.Select(b => b.CustomerId).Distinct().Count()
                };

                periodComparison = new PeriodComparison
                {
                    CurrentPeriod = currentPeriod,
                    PreviousPeriod = previousPeriod,
                    Growth = new GrowthData
                    {
                        RevenueGrowth = previousPeriod.Revenue > 0
                            ? (decimal)(((double)(currentPeriod.Revenue - previousPeriod.Revenue) / (double)previousPeriod.Revenue) * 100)
                            : (currentPeriod.Revenue > 0 ? 100 : 0),
                        BookingGrowth = previousPeriod.Bookings > 0
                            ? (decimal)(((currentPeriod.Bookings - previousPeriod.Bookings) / (double)previousPeriod.Bookings) * 100)
                            : (currentPeriod.Bookings > 0 ? 100 : 0),
                        CustomerGrowth = previousPeriod.Customers > 0
                            ? (decimal)(((currentPeriod.Customers - previousPeriod.Customers) / (double)previousPeriod.Customers) * 100)
                            : (currentPeriod.Customers > 0 ? 100 : 0)
                    }
                };
            }

            return new TimeBasedStatistics
            {
                Today = todayStats,
                Yesterday = yesterdayStats,
                ThisWeek = thisWeekStats,
                ThisMonth = thisMonthStats,
                ThisYear = thisYearStats,
                RevenueTrend = revenueTrend,
                PeriodComparison = periodComparison
            };
        }

        private TimePeriodStat CalculateTimePeriodStat(List<Booking> bookings, DateTime startDate, DateTime endDate)
        {
            var periodBookings = bookings.Where(b => b.BookingTime >= startDate && b.BookingTime < endDate).ToList();

            return new TimePeriodStat
            {
                Bookings = periodBookings.Count,
                Revenue = periodBookings.Sum(b => b.TotalAmount),
                Tickets = periodBookings.Sum(b => b.Tickets.Count),
                Customers = periodBookings.Select(b => b.CustomerId).Distinct().Count()
            };
        }

        private List<TimeSeriesData> CalculateRevenueTrend(List<Booking> bookings, DateTime fromDate, DateTime toDate, string groupBy)
        {
            var trend = new List<TimeSeriesData>();

            switch (groupBy.ToLower())
            {
                case "day":
                    var dailyGroups = bookings
                        .GroupBy(b => b.BookingTime.Date)
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = dailyGroups.Select(g => new TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;

                case "week":
                    var weeklyGroups = bookings
                        .GroupBy(b => GetWeekStart(b.BookingTime))
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = weeklyGroups.Select(g => new TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;

                case "month":
                    var monthlyGroups = bookings
                        .GroupBy(b => new DateTime(b.BookingTime.Year, b.BookingTime.Month, 1))
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = monthlyGroups.Select(g => new TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;
            }

            return trend;
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private VoucherStatistics CalculateVoucherStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var bookingsWithVoucher = paidBookings.Where(b => b.Voucher != null).ToList();

            var totalVouchersUsed = bookingsWithVoucher.Count;
            var totalVoucherDiscount = bookingsWithVoucher.Sum(b =>
            {
                if (b.Voucher == null) return 0;
                if (b.Voucher.DiscountType.ToLower() == "fixed")
                    return b.Voucher.DiscountVal;
                else // percent
                    return b.TotalAmount * (b.Voucher.DiscountVal / 100);
            });

            var voucherUsageRate = paidBookings.Any()
                ? (totalVouchersUsed / (double)paidBookings.Count) * 100
                : 0;

            var mostUsedVouchers = bookingsWithVoucher
                .Where(b => b.Voucher != null)
                .GroupBy(b => b.Voucher!.VoucherCode)
                .Select(g => new VoucherUsageStat
                {
                    VoucherCode = g.Key,
                    UsageCount = g.Count(),
                    TotalDiscount = g.Sum(b =>
                    {
                        var v = b.Voucher!;
                        if (v.DiscountType.ToLower() == "fixed")
                            return v.DiscountVal;
                        else
                            return b.TotalAmount * (v.DiscountVal / 100);
                    })
                })
                .OrderByDescending(v => v.UsageCount)
                .Take(10)
                .ToList();

            return new VoucherStatistics
            {
                TotalVouchersUsed = totalVouchersUsed,
                TotalVoucherDiscount = totalVoucherDiscount,
                VoucherUsageRate = (decimal)voucherUsageRate,
                MostUsedVouchers = mostUsedVouchers
            };
        }

        private PaymentStatistics CalculatePaymentStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var failedBookings = bookings.Where(b => b.PaymentStatus?.ToUpper() == "FAILED" || b.Status.ToUpper() == "FAILED").ToList();
            var pendingBookings = bookings.Where(b => b.PaymentStatus?.ToUpper() == "PENDING" || b.Status.ToUpper() == "PENDING_PAYMENT").ToList();

            var paymentByProvider = paidBookings
                .Where(b => !string.IsNullOrEmpty(b.PaymentProvider))
                .GroupBy(b => b.PaymentProvider)
                .Select(g => new PaymentProviderStat
                {
                    Provider = g.Key,
                    BookingCount = g.Count(),
                    TotalAmount = g.Sum(b => b.TotalAmount)
                })
                .ToList();

            var failedPaymentRate = bookings.Any()
                ? (failedBookings.Count / (double)bookings.Count) * 100
                : 0;

            var pendingPaymentAmount = pendingBookings.Sum(b => b.TotalAmount);

            return new PaymentStatistics
            {
                PaymentByProvider = paymentByProvider,
                FailedPaymentRate = (decimal)failedPaymentRate,
                PendingPaymentAmount = pendingPaymentAmount
            };
        }

        /// <summary>
        /// Get customers with successful bookings (Manager only)
        /// Returns top customers by booking count and total spent, plus full paginated list
        /// </summary>
        public async Task<SuccessfulBookingCustomersResponse> GetSuccessfulBookingCustomersAsync(
            int userId, 
            GetSuccessfulBookingCustomersRequest request)
        {
            // Validate manager user
            var manager = await _context.Managers
                .Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (manager == null || manager.User == null || !manager.User.IsActive || manager.User.UserType.ToLower() != "manager")
                throw new UnauthorizedException("Bạn không có quyền truy cập. Chỉ Manager mới có thể xem danh sách khách hàng.");

            // Validate request parameters
            if (request.TopLimit < 1 || request.TopLimit > 50)
                throw new ValidationException("topLimit", "TopLimit phải trong khoảng 1-50.");

            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            if (request.SortBy != "booking_count" && request.SortBy != "total_spent")
                throw new ValidationException("sortBy", "SortBy phải là 'booking_count' hoặc 'total_spent'.");

            var validSortOrder = new[] { "asc", "desc" };
            if (!validSortOrder.Contains(request.SortOrder.ToLower()))
                throw new ValidationException("sortOrder", "SortOrder phải là 'asc' hoặc 'desc'.");

            if (!validSortOrder.Contains(request.TopByBookingCountSortOrder.ToLower()))
                throw new ValidationException("topByBookingCountSortOrder", "TopByBookingCountSortOrder phải là 'asc' hoặc 'desc'.");

            if (!validSortOrder.Contains(request.TopByTotalSpentSortOrder.ToLower()))
                throw new ValidationException("topByTotalSpentSortOrder", "TopByTotalSpentSortOrder phải là 'asc' hoặc 'desc'.");

            if (request.FromDate.HasValue && request.ToDate.HasValue && request.ToDate.Value < request.FromDate.Value)
                throw new ValidationException("toDate", "ToDate phải lớn hơn hoặc bằng FromDate.");

            // 1. Query all successful bookings with filters
            var bookingsQuery = _context.Bookings
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                        .ThenInclude(c => c.Partner)
                .Where(b => b.PaymentStatus == "PAID" || 
                            (b.Status == "CONFIRMED" && b.PaymentStatus == "PAID") ||
                            (b.State == "COMPLETED" && b.PaymentStatus == "PAID"))
                .Where(b => b.Customer.User.IsActive == true);

            // Apply filters
            if (request.FromDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingTime >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingTime <= request.ToDate.Value);

            if (request.PartnerId.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.Showtime.Cinema.PartnerId == request.PartnerId.Value);

            if (request.CinemaId.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.Showtime.CinemaId == request.CinemaId.Value);

            if (!string.IsNullOrEmpty(request.CustomerEmail))
                bookingsQuery = bookingsQuery.Where(b => b.Customer.User.Email.Contains(request.CustomerEmail));

            if (!string.IsNullOrEmpty(request.CustomerName))
                bookingsQuery = bookingsQuery.Where(b => 
                    b.Customer.User.Fullname != null && 
                    b.Customer.User.Fullname.Contains(request.CustomerName));

            var paidBookings = await bookingsQuery.ToListAsync();

            // 2. Group by Customer và tính toán statistics
            var customerStats = paidBookings
                .GroupBy(b => new
                {
                    CustomerId = b.CustomerId,
                    UserId = b.Customer.UserId,
                    Fullname = b.Customer.User.Fullname,
                    Username = b.Customer.User.Username,
                    Email = b.Customer.User.Email,
                    Phone = b.Customer.User.Phone,
                    AvatarUrl = b.Customer.User.AvatarUrl
                })
                .Select(g => new CustomerBookingInfo
                {
                    CustomerId = g.Key.CustomerId,
                    UserId = g.Key.UserId,
                    Fullname = g.Key.Fullname,
                    Username = g.Key.Username,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    AvatarUrl = g.Key.AvatarUrl,
                    TotalBookings = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalAmount),
                    TotalTicketsPurchased = g.Sum(b => b.Tickets.Count),
                    AverageOrderValue = g.Any() ? g.Average(b => b.TotalAmount) : 0,
                    LastBookingDate = g.Max(b => b.BookingTime),
                    FirstBookingDate = g.Min(b => b.BookingTime)
                })
                .ToList();

            // 3. Tạo TopCustomersByBookingCount với sort order
            IEnumerable<CustomerBookingInfo> topByBookingCountQuery;
            if (request.TopByBookingCountSortOrder.ToLower() == "desc")
                topByBookingCountQuery = customerStats
                    .OrderByDescending(c => c.TotalBookings)
                    .ThenByDescending(c => c.TotalSpent);
            else
                topByBookingCountQuery = customerStats
                    .OrderBy(c => c.TotalBookings)
                    .ThenBy(c => c.TotalSpent);

            var topByBookingCount = topByBookingCountQuery
                .Take(request.TopLimit)
                .ToList();

            // 4. Tạo TopCustomersByTotalSpent với sort order
            IEnumerable<CustomerBookingInfo> topByTotalSpentQuery;
            if (request.TopByTotalSpentSortOrder.ToLower() == "desc")
                topByTotalSpentQuery = customerStats
                    .OrderByDescending(c => c.TotalSpent)
                    .ThenByDescending(c => c.TotalBookings);
            else
                topByTotalSpentQuery = customerStats
                    .OrderBy(c => c.TotalSpent)
                    .ThenBy(c => c.TotalBookings);

            var topByTotalSpent = topByTotalSpentQuery
                .Take(request.TopLimit)
                .ToList();

            // 5. Tạo FullCustomerList với pagination
            IEnumerable<CustomerBookingInfo> sortedCustomersQuery;
            if (request.SortBy == "booking_count")
            {
                if (request.SortOrder.ToLower() == "desc")
                    sortedCustomersQuery = customerStats
                        .OrderByDescending(c => c.TotalBookings)
                        .ThenByDescending(c => c.TotalSpent);
                else
                    sortedCustomersQuery = customerStats
                        .OrderBy(c => c.TotalBookings)
                        .ThenBy(c => c.TotalSpent);
            }
            else // total_spent
            {
                if (request.SortOrder.ToLower() == "desc")
                    sortedCustomersQuery = customerStats
                        .OrderByDescending(c => c.TotalSpent)
                        .ThenByDescending(c => c.TotalBookings);
                else
                    sortedCustomersQuery = customerStats
                        .OrderBy(c => c.TotalSpent)
                        .ThenBy(c => c.TotalBookings);
            }

            var totalCount = customerStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var paginatedCustomers = sortedCustomersQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var pagination = new PaginationMetadata
            {
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            // 6. Tính Statistics
            var maxBookings = customerStats.Any() ? customerStats.Max(c => c.TotalBookings) : 0;
            var minBookings = customerStats.Any() ? customerStats.Min(c => c.TotalBookings) : 0;
            var avgBookings = customerStats.Any() ? (decimal)customerStats.Average(c => c.TotalBookings) : 0;

            var customerWithMostBookings = customerStats
                .OrderByDescending(c => c.TotalBookings)
                .ThenByDescending(c => c.TotalSpent)
                .FirstOrDefault();

            var customerWithLeastBookings = customerStats
                .OrderBy(c => c.TotalBookings)
                .ThenBy(c => c.TotalSpent)
                .FirstOrDefault();

            return new SuccessfulBookingCustomersResponse
            {
                TopCustomersByBookingCount = topByBookingCount,
                TopCustomersByTotalSpent = topByTotalSpent,
                FullCustomerList = new PaginatedCustomerList
                {
                    Customers = paginatedCustomers,
                    Pagination = pagination
                },
                Statistics = new BookingCountStatistics
                {
                    MaxBookings = maxBookings,
                    MinBookings = minBookings,
                    AverageBookings = avgBookings,
                    CustomerWithMostBookings = customerWithMostBookings,
                    CustomerWithLeastBookings = customerWithLeastBookings
                },
                TotalCustomers = totalCount
            };
        }
    }
}