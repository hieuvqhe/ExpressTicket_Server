using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public interface ICashierService
{
    Task<ScanTicketResponse> ScanTicketAsync(string qrCode, int cashierEmployeeId, int cinemaId);
    Task<CheckInStatsResponse> GetCheckInStatsAsync(int showtimeId, int cashierEmployeeId, int cinemaId);
    Task<ChannelStatsResponse> GetChannelStatsAsync(int showtimeId, int cashierEmployeeId, int cinemaId);
    Task<CustomerBehaviorResponse> GetCustomerBehaviorAsync(int showtimeId, int cashierEmployeeId, int cinemaId);
    Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId, int cashierEmployeeId, int cinemaId);
    Task<CashierBookingsResponse> GetBookingsAsync(int cashierEmployeeId, int cinemaId, int page = 1, int pageSize = 20, string? status = null, string? paymentStatus = null, DateTime? fromDate = null, DateTime? toDate = null, string? bookingCode = null, string? orderCode = null, int? showtimeId = null, string sortBy = "booking_time", string sortOrder = "desc");
    Task<CashierCinemaResponse> GetMyCinemaAsync(int cashierEmployeeId);
}





