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
}

