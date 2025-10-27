using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.CinemaManagement.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.TheaterManagement.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using CinemaModel = ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models.Cinema;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class CinemaManagementService
    {
        private readonly CinemaDbCoreContext _context;

        public CinemaManagementService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách rạp chiếu (có phân trang, tìm kiếm, sắp xếp)
        /// </summary>
        public async Task<PaginatedCinemasResponse> GetCinemasAsync(
            int managerId, int page, int limit, string? search, string? sortBy, string? sortOrder)
        {
            var query = _context.Cinemas
                .Include(c => c.Partner)
                .AsQueryable();

            // 🔎 Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CinemaName.Contains(search) ||
                    c.Address.Contains(search));
            }

            // 🔁 Sắp xếp
            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("address", "desc") => query.OrderByDescending(c => c.Address),
                ("address", _) => query.OrderBy(c => c.Address),
                ("createdat", "desc") => query.OrderByDescending(c => c.CreatedAt),
                ("createdat", _) => query.OrderBy(c => c.CreatedAt),
                ("cinemaname", "desc") => query.OrderByDescending(c => c.CinemaName),
                _ => query.OrderBy(c => c.CinemaName)
            };

            // 📄 Phân trang
            var totalCount = await query.CountAsync();
            var cinemas = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new CinemaResponse
                {
                    CinemaId = c.CinemaId,
                    CinemaName = c.CinemaName,
                    Address = c.Address,
                    Phone = c.Phone,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new PaginatedCinemasResponse
            {
                Items = cinemas,
                TotalCount = totalCount,
                Page = page,
                Limit = limit
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết một rạp chiếu
        /// </summary>
        public async Task<CinemaResponse> GetCinemaByIdAsync(int id, int managerId)
        {
            var cinema = await _context.Cinemas.FindAsync(id);

            if (cinema == null)
                throw new NotFoundException($"Không tìm thấy rạp chiếu có ID = {id}");

            return new CinemaResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                Phone = cinema.Phone,
                CreatedAt = cinema.CreatedAt
            };
        }

        /// <summary>
        /// Tạo rạp chiếu mới
        /// </summary>
        public async Task<CinemaResponse> CreateCinemaAsync(int managerId, CreateCinemaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CinemaName))
                throw new ValidationException("", "Tên rạp không được để trống.", "");

            var exists = await _context.Cinemas.AnyAsync(c => c.CinemaName == request.CinemaName);
            if (exists)
                throw new ConflictException(""," đã tồn tại.", "");

            var cinema = new CinemaModel
            {
                CinemaName = request.CinemaName,
                Address = request.Address,
                Phone = request.Phone,
                PartnerId = request.PartnerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();

            return new CinemaResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                Phone = cinema.Phone,
                CreatedAt = cinema.CreatedAt
            };
        }

        /// <summary>
        /// Cập nhật thông tin rạp chiếu
        /// </summary>
        public async Task<CinemaResponse> UpdateCinemaAsync(int id, int managerId, UpdateCinemaRequest request)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
                throw new NotFoundException($"Không tìm thấy rạp chiếu có ID = {id}");

            // Kiểm tra trùng tên
            var duplicate = await _context.Cinemas
                .AnyAsync(c => c.CinemaName == request.CinemaName && c.CinemaId != id);
            if (duplicate)
                throw new ConflictException($"{request.CinemaName}", "Đã tồn tại", "Vui lòng thử lại");

            cinema.CinemaName = request.CinemaName;
            cinema.Address = request.Address;
            cinema.Phone = request.Phone;
            cinema.CreatedAt = cinema.CreatedAt; // giữ nguyên
            _context.Cinemas.Update(cinema);
            await _context.SaveChangesAsync();

            return new CinemaResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                Phone = cinema.Phone,
                CreatedAt = cinema.CreatedAt
            };
        }

        /// <summary>
        /// Xóa rạp chiếu
        /// </summary>
        public async Task DeleteCinemaAsync(int id, int managerId)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Showtimes)
                .FirstOrDefaultAsync(c => c.CinemaId == id);

            if (cinema == null)
                throw new NotFoundException($"Không tìm thấy rạp chiếu có ID = {id}");

            // Nếu có suất chiếu thì không cho xóa
            if (cinema.Showtimes.Any())
                throw new ConflictException("","","Không thể xóa rạp vì đang có suất chiếu hoạt động.");

            _context.Cinemas.Remove(cinema);
            await _context.SaveChangesAsync();
        }
    }
}
