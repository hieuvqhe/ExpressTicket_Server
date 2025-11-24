using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface ICinemaService
    {
        Task<CinemaResponse> CreateCinemaAsync(CreateCinemaRequest request, int partnerId, int userId);
        Task<CinemaResponse> GetCinemaByIdAsync(int cinemaId, int partnerId, int userId);
        Task<PaginatedCinemasResponse> GetCinemasAsync(int partnerId, int userId, int page = 1, int limit = 10,
            string? city = null, string? district = null, bool? isActive = null, string? search = null,
            string? sortBy = "cinema_name", string? sortOrder = "asc");
        Task<List<CinemaResponse>> GetAllCinemasForStaffAsync(int partnerId, int userId,
            string? city = null, string? district = null, bool? isActive = null, string? search = null,
            string? sortBy = "cinema_name", string? sortOrder = "asc");
        Task<CinemaResponse> UpdateCinemaAsync(int cinemaId, UpdateCinemaRequest request, int partnerId, int userId);
        Task<CinemaActionResponse> DeleteCinemaAsync(int cinemaId, int partnerId, int userId);
    }

    public class CinemaActionResponse
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}