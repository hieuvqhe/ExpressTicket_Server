using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class CashierService : ICashierService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;

        public CashierService(CinemaDbCoreContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<ScanTicketResponse> ScanTicketAsync(string qrCode, int cashierEmployeeId, int cinemaId)
        {
            // Validate cashier belongs to cinema
            var cashier = await _context.Employees
                .Include(e => e.CinemaAssignments)
                .FirstOrDefaultAsync(e => e.EmployeeId == cashierEmployeeId 
                    && e.RoleType == "Cashier" 
                    && e.IsActive);

            if (cashier == null)
            {
                throw new UnauthorizedException("Không tìm thấy thu ngân hoặc thu ngân không hoạt động");
            }

            var hasAccess = cashier.CinemaAssignments.Any(a => a.CinemaId == cinemaId && a.IsActive);
            if (!hasAccess)
            {
                throw new UnauthorizedException("Thu ngân không có quyền quét vé tại rạp này");
            }

            // Parse QR code: Format is "A6BK202511281407418972" where "A6" is SeatName and "BK202511281407418972" is BookingCode
            // Note: SeatName is the full name of the seat (e.g., "A6"), not RowCode + SeatNumber
            // A seat named "A6" might be in RowCode="A" but SeatNumber=7 (if seat 1 is "Z0")
            var (seatName, bookingCode) = ParseQrCode(qrCode);

            if (string.IsNullOrEmpty(seatName) || string.IsNullOrEmpty(bookingCode))
            {
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "QR code không hợp lệ. Format phải là: [SeatName][BookingCode] (ví dụ: A6BK202511281407418972)",
                    TicketInfo = null,
                    BookingStatus = null
                };
            }

            // Find ticket by SeatName (the full name stored in Seat.SeatName) and BookingCode
            var ticket = await _context.Tickets
                .Include(t => t.Seat)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Showtime)
                        .ThenInclude(s => s.Cinema)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Showtime)
                        .ThenInclude(s => s.Movie)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(t => 
                    t.Seat.SeatName == seatName
                    && t.Booking.BookingCode == bookingCode
                    && t.Booking.Showtime.CinemaId == cinemaId);

            if (ticket == null)
            {
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "Không tìm thấy vé. Vui lòng kiểm tra lại QR code hoặc vé không thuộc rạp này.",
                    TicketInfo = null,
                    BookingStatus = null
                };
            }

            // Validate ticket belongs to this cinema
            if (ticket.Booking.Showtime.CinemaId != cinemaId)
            {
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "Vé không thuộc rạp này. Vui lòng đến đúng rạp để quét vé.",
                    TicketInfo = null,
                    BookingStatus = null
                };
            }

            // Validate showtime hasn't started too long ago (optional: allow check-in within 30 minutes after start)
            var showtime = ticket.Booking.Showtime;
            var now = DateTime.UtcNow;
            var timeDiff = (now - showtime.ShowDatetime).TotalMinutes;
            
            // TEST: Allow scanning within 3 days before/after showtime
            // 3 days = 3 * 24 * 60 = 4320 minutes
            if (timeDiff < -4320) // Too early (more than 3 days before)
            {
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "Chưa đến thời gian quét vé. Vui lòng quét lại sau.",
                    TicketInfo = null,
                    BookingStatus = null
                };
            }
            
            // TEST: Allow scanning up to 3 days after showtime
            if (timeDiff > 4320) // Too late (more than 3 days after)
            {
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "Đã quá thời gian quét vé (quá 3 ngày sau khi phim bắt đầu).",
                    TicketInfo = null,
                    BookingStatus = null
                };
            }

            // Check if ticket already has SeatTicket record
            var seatTicket = await _context.SeatTickets
                .FirstOrDefaultAsync(st => st.TicketId == ticket.TicketId);

            bool isAlreadyCheckedIn = false;
            if (seatTicket != null && seatTicket.CheckInStatus == "CHECKED_IN")
            {
                isAlreadyCheckedIn = true;
                return new ScanTicketResponse
                {
                    Success = false,
                    Message = "Vé này đã được quét rồi.",
                    TicketInfo = new TicketScanInfo
                    {
                        TicketId = ticket.TicketId,
                        SeatId = ticket.SeatId,
                        SeatName = ticket.Seat.SeatName ?? $"{ticket.Seat.RowCode}{ticket.Seat.SeatNumber}",
                        OrderCode = ticket.Booking.OrderCode ?? "",
                        BookingId = ticket.BookingId,
                        BookingCode = ticket.Booking.BookingCode,
                        ShowtimeId = ticket.ShowtimeId,
                        ShowtimeStart = showtime.ShowDatetime,
                        MovieName = showtime.Movie.Title ?? "",
                        CinemaName = showtime.Cinema.CinemaName ?? "",
                        CheckInTime = seatTicket.CheckInTime,
                        IsAlreadyCheckedIn = true
                    },
                    BookingStatus = await GetBookingCheckInStatusAsync(ticket.BookingId)
                };
            }

            // Create or update SeatTicket
            if (seatTicket == null)
            {
                seatTicket = new SeatTicket
                {
                    TicketId = ticket.TicketId,
                    BookingId = ticket.BookingId,
                    SeatId = ticket.SeatId,
                    ShowtimeId = ticket.ShowtimeId,
                    OrderCode = ticket.Booking.OrderCode ?? "",
                    CheckInStatus = "CHECKED_IN",
                    CheckInTime = now,
                    CheckedInBy = cashierEmployeeId,
                    CinemaId = cinemaId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.SeatTickets.Add(seatTicket);
            }
            else
            {
                seatTicket.CheckInStatus = "CHECKED_IN";
                seatTicket.CheckInTime = now;
                seatTicket.CheckedInBy = cashierEmployeeId;
                seatTicket.UpdatedAt = now;
            }

            // Update ticket status
            ticket.Status = "CHECKED_IN";

            // Check if all tickets in booking are checked in
            var allTickets = await _context.Tickets
                .Where(t => t.BookingId == ticket.BookingId)
                .ToListAsync();

            var allSeatTickets = await _context.SeatTickets
                .Where(st => st.BookingId == ticket.BookingId)
                .ToListAsync();

            var checkedInCount = allSeatTickets.Count(st => st.CheckInStatus == "CHECKED_IN");
            var totalTickets = allTickets.Count;

            // Update booking status based on check-in status
            var booking = ticket.Booking;
            if (checkedInCount == totalTickets)
            {
                booking.Status = "FULLY_CHECKED_IN";
            }
            else if (checkedInCount > 0)
            {
                booking.Status = "PARTIAL_CHECKED_IN";
            }

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEntityChangeAsync(
                action: "CASHIER_SCAN_TICKET",
                tableName: "SeatTicket",
                recordId: seatTicket.SeatTicketId,
                beforeData: null,
                afterData: new { seatTicket.SeatTicketId, ticket.TicketId, CheckInTime = now, CheckedInBy = cashierEmployeeId },
                metadata: new { qrCode, cinemaId, cashierEmployeeId });

            return new ScanTicketResponse
            {
                Success = true,
                Message = "Quét vé thành công!",
                    TicketInfo = new TicketScanInfo
                    {
                        TicketId = ticket.TicketId,
                        SeatId = ticket.SeatId,
                        SeatName = ticket.Seat.SeatName ?? $"{ticket.Seat.RowCode}{ticket.Seat.SeatNumber}",
                        OrderCode = ticket.Booking.OrderCode ?? "",
                        BookingId = ticket.BookingId,
                        BookingCode = ticket.Booking.BookingCode,
                        ShowtimeId = ticket.ShowtimeId,
                        ShowtimeStart = showtime.ShowDatetime,
                        MovieName = showtime.Movie.Title ?? "",
                        CinemaName = showtime.Cinema.CinemaName ?? "",
                        CheckInTime = now,
                        IsAlreadyCheckedIn = false
                    },
                BookingStatus = await GetBookingCheckInStatusAsync(ticket.BookingId)
            };
        }

        /// <summary>
        /// Parse QR code format: "A6BK202511281407418972" -> (seatName: "A6", bookingCode: "BK202511281407418972")
        /// QR code format: [SeatName][BookingCode] where SeatName is like "A6", "B12", "AA5" and BookingCode starts with "BK"
        /// </summary>
        private (string seatName, string bookingCode) ParseQrCode(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return ("", "");
            }

            // Try to find "BK" in the string (case-insensitive) - BookingCode usually starts with "BK"
            var bkIndex = qrCode.IndexOf("BK", StringComparison.OrdinalIgnoreCase);
            
            if (bkIndex > 0)
            {
                // Found BK, split at that position
                var seatName = qrCode.Substring(0, bkIndex).Trim();
                var bookingCode = qrCode.Substring(bkIndex).Trim();
                
                // Validate: seatName should have at least 1 character
                // bookingCode should start with "BK" (case-insensitive)
                if (!string.IsNullOrEmpty(seatName) && 
                    !string.IsNullOrEmpty(bookingCode) &&
                    bookingCode.StartsWith("BK", StringComparison.OrdinalIgnoreCase))
                {
                    return (seatName, bookingCode);
                }
            }

            // If no "BK" found or format invalid, return empty
            return ("", "");
        }


        private async Task<BookingCheckInStatus> GetBookingCheckInStatusAsync(int bookingId)
        {
            var allTickets = await _context.Tickets
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();

            var allSeatTickets = await _context.SeatTickets
                .Where(st => st.BookingId == bookingId)
                .ToListAsync();

            var checkedInCount = allSeatTickets.Count(st => st.CheckInStatus == "CHECKED_IN");
            var totalTickets = allTickets.Count;

            string bookingStatus;
            if (checkedInCount == totalTickets)
            {
                bookingStatus = "FULLY_CHECKED_IN";
            }
            else if (checkedInCount > 0)
            {
                bookingStatus = "PARTIAL_CHECKED_IN";
            }
            else
            {
                bookingStatus = "CONFIRMED";
            }

            return new BookingCheckInStatus
            {
                BookingId = bookingId,
                TotalTickets = totalTickets,
                CheckedInTickets = checkedInCount,
                NotCheckedInTickets = totalTickets - checkedInCount,
                BookingStatus = bookingStatus
            };
        }

        public async Task<CheckInStatsResponse> GetCheckInStatsAsync(int showtimeId, int cashierEmployeeId, int cinemaId)
        {
            // Validate cashier access
            await ValidateCashierAccessAsync(cashierEmployeeId, cinemaId);

            // Validate showtime belongs to cinema
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.CinemaId == cinemaId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc rạp này");
            }

            // Get all tickets for this showtime
            var allTickets = await _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId)
                .ToListAsync();

            var totalTicketsSold = allTickets.Count;

            // Get all checked-in tickets
            var checkedInTickets = await _context.SeatTickets
                .Where(st => st.ShowtimeId == showtimeId && st.CheckInStatus == "CHECKED_IN")
                .ToListAsync();

            var totalTicketsCheckedIn = checkedInTickets.Count;
            var noShowCount = totalTicketsSold - totalTicketsCheckedIn;

            var checkInRate = totalTicketsSold > 0 
                ? (decimal)totalTicketsCheckedIn / totalTicketsSold * 100 
                : 0;
            var noShowRate = totalTicketsSold > 0 
                ? (decimal)noShowCount / totalTicketsSold * 100 
                : 0;

            return new CheckInStatsResponse
            {
                ShowtimeId = showtimeId,
                MovieName = showtime.Movie.Title ?? "",
                ShowtimeStart = showtime.ShowDatetime,
                TotalTicketsSold = totalTicketsSold,
                TotalTicketsCheckedIn = totalTicketsCheckedIn,
                NoShowCount = noShowCount,
                CheckInRate = Math.Round(checkInRate, 2),
                NoShowRate = Math.Round(noShowRate, 2),
                OccupancyActual = totalTicketsCheckedIn,
                OccupancySold = totalTicketsSold
            };
        }

        public async Task<ChannelStatsResponse> GetChannelStatsAsync(int showtimeId, int cashierEmployeeId, int cinemaId)
        {
            // Validate cashier access
            await ValidateCashierAccessAsync(cashierEmployeeId, cinemaId);

            // Validate showtime belongs to cinema
            var showtime = await _context.Showtimes
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.CinemaId == cinemaId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc rạp này");
            }

            // Get all bookings for this showtime
            var bookings = await _context.Bookings
                .Include(b => b.Tickets)
                .Where(b => b.ShowtimeId == showtimeId)
                .ToListAsync();

            // Group by channel (PaymentProvider)
            var channelGroups = bookings
                .GroupBy(b => GetChannelName(b.PaymentProvider))
                .Select(g => new
                {
                    ChannelName = g.Key,
                    Bookings = g.ToList()
                })
                .ToList();

            var channelStats = new List<ChannelStat>();

            foreach (var group in channelGroups)
            {
                var ticketsSold = group.Bookings.Sum(b => b.Tickets.Count);
                
                var bookingIds = group.Bookings.Select(b => b.BookingId).ToList();
                var checkedInTickets = await _context.SeatTickets
                    .Where(st => bookingIds.Contains(st.BookingId) && st.CheckInStatus == "CHECKED_IN")
                    .CountAsync();

                var noShowCount = ticketsSold - checkedInTickets;
                var checkInRate = ticketsSold > 0 
                    ? (decimal)checkedInTickets / ticketsSold * 100 
                    : 0;
                var noShowRate = ticketsSold > 0 
                    ? (decimal)noShowCount / ticketsSold * 100 
                    : 0;

                channelStats.Add(new ChannelStat
                {
                    ChannelName = group.ChannelName,
                    TicketsSold = ticketsSold,
                    TicketsCheckedIn = checkedInTickets,
                    NoShowCount = noShowCount,
                    CheckInRate = Math.Round(checkInRate, 2),
                    NoShowRate = Math.Round(noShowRate, 2)
                });
            }

            return new ChannelStatsResponse
            {
                ShowtimeId = showtimeId,
                Channels = channelStats
            };
        }

        public async Task<CustomerBehaviorResponse> GetCustomerBehaviorAsync(int showtimeId, int cashierEmployeeId, int cinemaId)
        {
            // Validate cashier access
            await ValidateCashierAccessAsync(cashierEmployeeId, cinemaId);

            // Validate showtime belongs to cinema
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.CinemaId == cinemaId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc rạp này");
            }

            // Get all checked-in tickets with check-in time
            var checkedInTickets = await _context.SeatTickets
                .Where(st => st.ShowtimeId == showtimeId 
                    && st.CheckInStatus == "CHECKED_IN" 
                    && st.CheckInTime.HasValue)
                .Select(st => new { CheckInTime = st.CheckInTime!.Value })
                .ToListAsync();

            var totalCheckIns = checkedInTickets.Count;
            var showtimeStart = showtime.ShowDatetime;

            int earlyArrivals = 0; // > 30 minutes before
            int onTimeArrivals = 0; // 0-30 minutes before
            int lateArrivals = 0; // after showtime start

            var timeRanges = new List<CheckInTimeRange>();

            foreach (var ticket in checkedInTickets)
            {
                var timeDiff = (showtimeStart - ticket.CheckInTime).TotalMinutes;

                if (timeDiff > 30)
                {
                    earlyArrivals++;
                }
                else if (timeDiff >= 0)
                {
                    onTimeArrivals++;
                }
                else
                {
                    lateArrivals++;
                }
            }

            var earlyRate = totalCheckIns > 0 ? (decimal)earlyArrivals / totalCheckIns * 100 : 0;
            var onTimeRate = totalCheckIns > 0 ? (decimal)onTimeArrivals / totalCheckIns * 100 : 0;
            var lateRate = totalCheckIns > 0 ? (decimal)lateArrivals / totalCheckIns * 100 : 0;

            timeRanges.Add(new CheckInTimeRange
            {
                Range = ">30m before",
                Count = earlyArrivals,
                Percentage = Math.Round(earlyRate, 2)
            });

            timeRanges.Add(new CheckInTimeRange
            {
                Range = "0-30m before",
                Count = onTimeArrivals,
                Percentage = Math.Round(onTimeRate, 2)
            });

            timeRanges.Add(new CheckInTimeRange
            {
                Range = "after start",
                Count = lateArrivals,
                Percentage = Math.Round(lateRate, 2)
            });

            return new CustomerBehaviorResponse
            {
                ShowtimeId = showtimeId,
                ShowtimeStart = showtimeStart,
                Stats = new BehaviorStats
                {
                    TotalCheckIns = totalCheckIns,
                    EarlyArrivals = earlyArrivals,
                    OnTimeArrivals = onTimeArrivals,
                    LateArrivals = lateArrivals,
                    EarlyArrivalRate = Math.Round(earlyRate, 2),
                    OnTimeArrivalRate = Math.Round(onTimeRate, 2),
                    LateArrivalRate = Math.Round(lateRate, 2),
                    TimeRanges = timeRanges
                }
            };
        }

        public async Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId, int cashierEmployeeId, int cinemaId)
        {
            // Validate cashier access
            await ValidateCashierAccessAsync(cashierEmployeeId, cinemaId);

            // Get booking with all related data
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Screen)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                throw new NotFoundException("Không tìm thấy booking");
            }

            // Validate booking belongs to cashier's cinema
            if (booking.Showtime.CinemaId != cinemaId)
            {
                throw new UnauthorizedException("Booking không thuộc rạp của bạn");
            }

            // Get all seat tickets for this booking
            var seatTickets = await _context.SeatTickets
                .Include(st => st.CheckedInByEmployee)
                .Where(st => st.BookingId == bookingId)
                .ToListAsync();

            var ticketDetails = new List<TicketDetail>();
            foreach (var ticket in booking.Tickets)
            {
                var seatTicket = seatTickets.FirstOrDefault(st => st.TicketId == ticket.TicketId);
                ticketDetails.Add(new TicketDetail
                {
                    TicketId = ticket.TicketId,
                    SeatId = ticket.SeatId,
                    SeatName = $"{ticket.Seat.RowCode}{ticket.Seat.SeatNumber}",
                    Price = ticket.Price,
                    Status = ticket.Status,
                    CheckInTime = seatTicket?.CheckInTime,
                    CheckedInByEmployeeName = seatTicket?.CheckedInByEmployee?.FullName,
                    CheckInStatus = seatTicket?.CheckInStatus ?? "NOT_CHECKED_IN"
                });
            }

            var totalTickets = booking.Tickets.Count;
            var checkedInTickets = seatTickets.Count(st => st.CheckInStatus == "CHECKED_IN");
            var notCheckedInTickets = totalTickets - checkedInTickets;

            string bookingCheckInStatus;
            if (checkedInTickets == totalTickets)
            {
                bookingCheckInStatus = "FULLY_CHECKED_IN";
            }
            else if (checkedInTickets > 0)
            {
                bookingCheckInStatus = "PARTIAL_CHECKED_IN";
            }
            else
            {
                bookingCheckInStatus = "CONFIRMED";
            }

            return new BookingDetailsResponse
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                OrderCode = booking.OrderCode,
                Status = booking.Status,
                BookingTime = booking.BookingTime,
                TotalAmount = booking.TotalAmount,
                PaymentProvider = booking.PaymentProvider,
                PaymentStatus = booking.PaymentStatus,
                Showtime = new ShowtimeInfo
                {
                    ShowtimeId = booking.ShowtimeId,
                    ShowDatetime = booking.Showtime.ShowDatetime,
                    MovieName = booking.Showtime.Movie.Title ?? "",
                    CinemaName = booking.Showtime.Cinema.CinemaName ?? "",
                    ScreenName = booking.Showtime.Screen.ScreenName ?? ""
                },
                Customer = new CustomerInfo
                {
                    CustomerId = booking.CustomerId,
                    FullName = booking.Customer.User.Fullname ?? "",
                    Email = booking.Customer.User.Email,
                    Phone = booking.Customer.User.Phone
                },
                Tickets = ticketDetails,
                CheckInSummary = new BookingCheckInSummary
                {
                    TotalTickets = totalTickets,
                    CheckedInTickets = checkedInTickets,
                    NotCheckedInTickets = notCheckedInTickets,
                    BookingCheckInStatus = bookingCheckInStatus
                }
            };
        }

        public async Task<CashierBookingsResponse> GetBookingsAsync(
            int cashierEmployeeId, 
            int cinemaId, 
            int page = 1, 
            int pageSize = 20, 
            string? status = null, 
            string? paymentStatus = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string? bookingCode = null, 
            string? orderCode = null, 
            int? showtimeId = null, 
            string sortBy = "booking_time", 
            string sortOrder = "desc")
        {
            // Validate cashier access
            await ValidateCashierAccessAsync(cashierEmployeeId, cinemaId);

            // Validate pagination
            if (page < 1)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["page"] = new ValidationError
                    {
                        Msg = "Page phải lớn hơn hoặc bằng 1.",
                        Path = "page",
                        Location = "query"
                    }
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["pageSize"] = new ValidationError
                    {
                        Msg = "PageSize phải trong khoảng 1-100.",
                        Path = "pageSize",
                        Location = "query"
                    }
                });
            }

            // Build query - chỉ lấy booking của rạp này
            var query = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                .Where(b => b.Showtime.CinemaId == cinemaId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(b => b.PaymentStatus == paymentStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BookingTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.BookingTime <= toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(bookingCode))
            {
                query = query.Where(b => b.BookingCode.Contains(bookingCode));
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                query = query.Where(b => b.OrderCode != null && b.OrderCode.Contains(orderCode));
            }

            if (showtimeId.HasValue)
            {
                query = query.Where(b => b.ShowtimeId == showtimeId.Value);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "booking_time" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(b => b.BookingTime)
                    : query.OrderByDescending(b => b.BookingTime),
                "total_amount" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(b => b.TotalAmount)
                    : query.OrderByDescending(b => b.TotalAmount),
                "booking_code" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(b => b.BookingCode)
                    : query.OrderByDescending(b => b.BookingCode),
                _ => query.OrderByDescending(b => b.BookingTime)
            };

            // Get total count
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Apply pagination
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var items = new List<CashierBookingItemDto>();
            foreach (var booking in bookings)
            {
                // Count tickets and check-in status
                var tickets = booking.Tickets.ToList();
                var ticketCount = tickets.Count;
                var checkedInTickets = tickets.Count(t => t.Status == "CHECKED_IN");
                var notCheckedInTickets = ticketCount - checkedInTickets;

                items.Add(new CashierBookingItemDto
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    OrderCode = booking.OrderCode,
                    BookingTime = booking.BookingTime,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    PaymentStatus = booking.PaymentStatus,
                    PaymentProvider = booking.PaymentProvider,
                    TicketCount = ticketCount,
                    CheckedInTicketCount = checkedInTickets,
                    NotCheckedInTicketCount = notCheckedInTickets,
                    Showtime = new CashierBookingShowtimeDto
                    {
                        ShowtimeId = booking.ShowtimeId,
                        ShowDatetime = booking.Showtime.ShowDatetime,
                        EndTime = booking.Showtime.EndTime,
                        FormatType = booking.Showtime.FormatType,
                        Status = booking.Showtime.Status ?? ""
                    },
                    Movie = new CashierBookingMovieDto
                    {
                        MovieId = booking.Showtime.Movie.MovieId,
                        Title = booking.Showtime.Movie.Title ?? "",
                        DurationMinutes = booking.Showtime.Movie.DurationMinutes,
                        PosterUrl = booking.Showtime.Movie.PosterUrl
                    },
                    Customer = new CashierBookingCustomerDto
                    {
                        CustomerId = booking.CustomerId,
                        FullName = booking.Customer.User.Fullname,
                        Email = booking.Customer.User.Email,
                        Phone = booking.Customer.User.Phone
                    }
                });
            }

            return new CashierBookingsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }

        private async Task ValidateCashierAccessAsync(int cashierEmployeeId, int cinemaId)
        {
            var cashier = await _context.Employees
                .Include(e => e.CinemaAssignments)
                .FirstOrDefaultAsync(e => e.EmployeeId == cashierEmployeeId 
                    && e.RoleType == "Cashier" 
                    && e.IsActive);

            if (cashier == null)
            {
                throw new UnauthorizedException("Không tìm thấy thu ngân hoặc thu ngân không hoạt động");
            }

            var hasAccess = cashier.CinemaAssignments.Any(a => a.CinemaId == cinemaId && a.IsActive);
            if (!hasAccess)
            {
                throw new UnauthorizedException("Thu ngân không có quyền truy cập rạp này");
            }
        }

        private string GetChannelName(string? paymentProvider)
        {
            if (string.IsNullOrWhiteSpace(paymentProvider))
            {
                return "Counter"; // Quầy rạp (không có payment provider)
            }

            var provider = paymentProvider.ToLower();
            
            // Có thể mở rộng logic này dựa trên payment provider
            // Ví dụ: nếu có UserId thì là App/Website, nếu không có thì là Partner
            return provider switch
            {
                "payos" => "App/Website", // PayOS thường dùng cho online
                "cash" => "Counter",
                "partner" => "Partner",
                _ => "Other"
            };
        }

        public async Task<CashierCinemaResponse> GetMyCinemaAsync(int cashierEmployeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.CinemaAssignments)
                    .ThenInclude(a => a.Cinema)
                .FirstOrDefaultAsync(e => e.EmployeeId == cashierEmployeeId 
                    && e.RoleType == "Cashier" 
                    && e.IsActive);

            if (employee == null)
            {
                throw new UnauthorizedException("Không tìm thấy thu ngân hoặc thu ngân không hoạt động");
            }

            var assignment = employee.CinemaAssignments.FirstOrDefault(a => a.IsActive);
            if (assignment == null || assignment.Cinema == null)
            {
                throw new UnauthorizedException("Thu ngân chưa được phân quyền rạp nào");
            }

            var cinema = assignment.Cinema;

            return new CashierCinemaResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName ?? string.Empty,
                Address = cinema.Address ?? string.Empty,
                Phone = cinema.Phone,
                Code = cinema.Code ?? string.Empty,
                City = cinema.City ?? string.Empty,
                District = cinema.District ?? string.Empty,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude,
                Email = cinema.Email,
                IsActive = cinema.IsActive ?? false,
                LogoUrl = cinema.LogoUrl,
                AssignedAt = assignment.AssignedAt,
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName
            };
        }
    }
}

