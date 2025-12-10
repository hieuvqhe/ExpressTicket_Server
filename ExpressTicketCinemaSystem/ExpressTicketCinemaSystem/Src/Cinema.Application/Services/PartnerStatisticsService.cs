using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class PartnerStatisticsService : IPartnerStatisticsService
    {
        private readonly CinemaDbCoreContext _context;

        public PartnerStatisticsService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<CheckInStatsResponse> GetCheckInStatsAsync(int showtimeId, int partnerId)
        {
            // Validate showtime belongs to partner's cinema
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.Cinema.PartnerId == partnerId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc các rạp của bạn");
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

        public async Task<ChannelStatsResponse> GetChannelStatsAsync(int showtimeId, int partnerId)
        {
            // Validate showtime belongs to partner's cinema
            var showtime = await _context.Showtimes
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.Cinema.PartnerId == partnerId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc các rạp của bạn");
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

        public async Task<CustomerBehaviorResponse> GetCustomerBehaviorAsync(int showtimeId, int partnerId)
        {
            // Validate showtime belongs to partner's cinema
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId && s.Cinema.PartnerId == partnerId);

            if (showtime == null)
            {
                throw new NotFoundException("Không tìm thấy suất chiếu hoặc suất chiếu không thuộc các rạp của bạn");
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

        public async Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId, int partnerId)
        {
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

            // Validate booking belongs to partner's cinema
            if (booking.Showtime.Cinema.PartnerId != partnerId)
            {
                throw new UnauthorizedException("Booking không thuộc các rạp của bạn");
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
                    SeatName = ticket.Seat.SeatName ?? $"{ticket.Seat.RowCode}{ticket.Seat.SeatNumber}",
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

        public async Task<List<CheckInStatsResponse>> GetCheckInStatsByCinemaAsync(int cinemaId, int partnerId, DateTime? startDate, DateTime? endDate)
        {
            // Validate cinema belongs to partner
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp hoặc rạp không thuộc quyền quản lý của bạn");
            }

            // Get showtimes for this cinema within date range
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .Where(s => s.CinemaId == cinemaId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.ShowDatetime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.ShowDatetime <= endDate.Value);
            }

            var showtimes = await query.ToListAsync();

            var results = new List<CheckInStatsResponse>();

            foreach (var showtime in showtimes)
            {
                var allTickets = await _context.Tickets
                    .Where(t => t.ShowtimeId == showtime.ShowtimeId)
                    .ToListAsync();

                var totalTicketsSold = allTickets.Count;

                var checkedInTickets = await _context.SeatTickets
                    .Where(st => st.ShowtimeId == showtime.ShowtimeId && st.CheckInStatus == "CHECKED_IN")
                    .CountAsync();

                var totalTicketsCheckedIn = checkedInTickets;
                var noShowCount = totalTicketsSold - totalTicketsCheckedIn;

                var checkInRate = totalTicketsSold > 0 
                    ? (decimal)totalTicketsCheckedIn / totalTicketsSold * 100 
                    : 0;
                var noShowRate = totalTicketsSold > 0 
                    ? (decimal)noShowCount / totalTicketsSold * 100 
                    : 0;

                results.Add(new CheckInStatsResponse
                {
                    ShowtimeId = showtime.ShowtimeId,
                    MovieName = showtime.Movie.Title ?? "",
                    ShowtimeStart = showtime.ShowDatetime,
                    TotalTicketsSold = totalTicketsSold,
                    TotalTicketsCheckedIn = totalTicketsCheckedIn,
                    NoShowCount = noShowCount,
                    CheckInRate = Math.Round(checkInRate, 2),
                    NoShowRate = Math.Round(noShowRate, 2),
                    OccupancyActual = totalTicketsCheckedIn,
                    OccupancySold = totalTicketsSold
                });
            }

            return results;
        }

        private string GetChannelName(string? paymentProvider)
        {
            if (string.IsNullOrWhiteSpace(paymentProvider))
            {
                return "Counter"; // Quầy rạp (không có payment provider)
            }

            var provider = paymentProvider.ToLower();
            
            return provider switch
            {
                "payos" => "App/Website", // PayOS thường dùng cho online
                "cash" => "Counter",
                "partner" => "Partner",
                _ => "Other"
            };
        }
    }
}















