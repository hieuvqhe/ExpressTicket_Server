using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services;

public interface IPartnerStatisticsService
{
    Task<CheckInStatsResponse> GetCheckInStatsAsync(int showtimeId, int partnerId);
    Task<ChannelStatsResponse> GetChannelStatsAsync(int showtimeId, int partnerId);
    Task<CustomerBehaviorResponse> GetCustomerBehaviorAsync(int showtimeId, int partnerId);
    Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId, int partnerId);
    Task<List<CheckInStatsResponse>> GetCheckInStatsByCinemaAsync(int cinemaId, int partnerId, DateTime? startDate, DateTime? endDate);
}















