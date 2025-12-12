using System;
using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface ISeatLockAppService
    {
        Task<LockSeatsResponse> LockAsync(Guid sessionId, LockSeatsRequest request, CancellationToken ct = default);
        Task<ReleaseSeatsResponse> ReleaseAsync(Guid sessionId, ReleaseSeatsRequest request, CancellationToken ct = default);
        Task<ReplaceSeatsResponse> ReplaceAsync(Guid sessionId, ReplaceSeatsRequest request, CancellationToken ct = default);
        Task<ValidateSeatsResponse> ValidateAsync(Guid sessionId, CancellationToken ct = default);
    }
}
