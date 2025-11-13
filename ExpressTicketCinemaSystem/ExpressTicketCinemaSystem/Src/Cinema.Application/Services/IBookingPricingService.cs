using System.Security.Claims;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IBookingPricingService
    {
        Task<ApplyCouponResponse> ApplyCouponAsync(Guid sessionId, ClaimsPrincipal user, ApplyCouponRequest req, CancellationToken ct = default);
    }

    public interface IBookingCheckoutService
    {
        Task<CheckoutResponse> CheckoutAsync(Guid sessionId, CheckoutRequest req, CancellationToken ct = default);
    }
}
