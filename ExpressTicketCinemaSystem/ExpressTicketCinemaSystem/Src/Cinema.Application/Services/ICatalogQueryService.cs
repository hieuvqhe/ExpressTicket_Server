using System.Threading;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface ICatalogQueryService
    {
        Task<MovieShowtimesOverviewResponse> GetMovieShowtimesOverviewAsync(
            int movieId, GetMovieShowtimesOverviewQuery query, CancellationToken ct = default);

        Task<ShowtimeSeatMapResponse> GetShowtimeSeatMapAsync(
            int showtimeId, CancellationToken ct = default);
    }
}
