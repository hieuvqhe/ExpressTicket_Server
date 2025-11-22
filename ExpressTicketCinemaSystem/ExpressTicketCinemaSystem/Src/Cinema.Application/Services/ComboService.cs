using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IComboService
    {
        Task<ServiceResponse> CreateAsync(int partnerId, int userId, CreateServiceRequest request);
        Task<ServiceResponse> GetByIdAsync(int partnerId, int userId, int serviceId);
        Task<PaginatedServicesResponse> GetListAsync(int partnerId, int userId, GetServicesQuery query);
        Task<ServiceResponse> UpdateAsync(int partnerId, int userId, int serviceId, UpdateServiceRequest request);
        Task<ServiceActionResponse> DeleteAsync(int partnerId, int userId, int serviceId);
    }

    public class ComboService : IComboService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IContractValidationService _contractValidation;

        public ComboService(CinemaDbCoreContext context, IContractValidationService contractValidation)
        {
            _context = context;
            _contractValidation = contractValidation;
        }

        // ====== CREATE ======
        public async Task<ServiceResponse> CreateAsync(int partnerId, int userId, CreateServiceRequest request)
        {
            await ValidatePartnerAccessAsync(partnerId, userId);
            await _contractValidation.ValidatePartnerHasActiveContractAsync(partnerId);
            ValidateCreate(request);
            await ValidateUniqueCodeAsync(partnerId, request.Code);

            var svc = new Service
            {
                PartnerId = partnerId,
                ServiceName = request.Name.Trim(),
                Code = request.Code.Trim().ToUpper(),
                Price = request.Price,
                Description = Normalize(request.Description),
                ImageUrl = Normalize(request.ImageUrl),
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Services.Add(svc);
            await _context.SaveChangesAsync();
            return Map(svc);
        }

        // ====== GET BY ID ======
        public async Task<ServiceResponse> GetByIdAsync(int partnerId, int userId, int serviceId)
        {
            await ValidatePartnerAccessAsync(partnerId, userId);

            var svc = await _context.Services
                .FirstOrDefaultAsync(x => x.ServiceId == serviceId && x.PartnerId == partnerId);

            if (svc == null)
                throw new NotFoundException("Không tìm thấy combo với ID này hoặc không thuộc quyền quản lý của bạn");

            return Map(svc);
        }

        // ====== LIST ======
        public async Task<PaginatedServicesResponse> GetListAsync(int partnerId, int userId, GetServicesQuery q)
        {
            await ValidatePartnerAccessAsync(partnerId, userId);
            ValidateListQuery(q);

            var query = _context.Services.Where(x => x.PartnerId == partnerId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim().ToLower();
                query = query.Where(x => x.ServiceName.ToLower().Contains(s) || x.Code.ToLower().Contains(s));
            }
            if (q.IsAvailable.HasValue)
                query = query.Where(x => x.IsAvailable == q.IsAvailable.Value);

            // sorting
            var sortBy = (q.SortBy ?? "created_at").ToLower();
            var asc = (q.SortOrder ?? "desc").ToLower() == "asc";
            query = sortBy switch
            {
                "name" => asc ? query.OrderBy(x => x.ServiceName) : query.OrderByDescending(x => x.ServiceName),
                "price" => asc ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price),
                "code" => asc ? query.OrderBy(x => x.Code) : query.OrderByDescending(x => x.Code),
                _ => asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((q.Page - 1) * q.Limit)
                .Take(q.Limit)
                .ToListAsync();

            return new PaginatedServicesResponse
            {
                Services = items.Select(Map).ToList(),
                Pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = q.Page,
                    PageSize = q.Limit,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)q.Limit)
                }
            };
        }

        // ====== UPDATE ======
        public async Task<ServiceResponse> UpdateAsync(int partnerId, int userId, int serviceId, UpdateServiceRequest request)
        {
            await ValidatePartnerAccessAsync(partnerId, userId);
            await _contractValidation.ValidatePartnerHasActiveContractAsync(partnerId);
            ValidateUpdate(request);

            var svc = await _context.Services
                .FirstOrDefaultAsync(x => x.ServiceId == serviceId && x.PartnerId == partnerId);

            if (svc == null)
                throw new NotFoundException("Không tìm thấy combo với ID này hoặc không thuộc quyền quản lý của bạn");

            svc.ServiceName = request.Name.Trim();
            svc.Price = request.Price;
            svc.Description = Normalize(request.Description);
            svc.ImageUrl = Normalize(request.ImageUrl);
            svc.IsAvailable = request.IsAvailable;
            svc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Map(svc);
        }

        // ====== DELETE (SOFT) ======
        public async Task<ServiceActionResponse> DeleteAsync(int partnerId, int userId, int serviceId)
        {
            await ValidatePartnerAccessAsync(partnerId, userId);
            await _contractValidation.ValidatePartnerHasActiveContractAsync(partnerId);

            var svc = await _context.Services
                .FirstOrDefaultAsync(x => x.ServiceId == serviceId && x.PartnerId == partnerId);

            if (svc == null)
                throw new NotFoundException("Không tìm thấy combo với ID này hoặc không thuộc quyền quản lý của bạn");

            // Soft delete: set IsAvailable = false
            if (svc.IsAvailable)
            {
                svc.IsAvailable = false;
                svc.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new ServiceActionResponse
            {
                ServiceId = svc.ServiceId,
                IsAvailable = svc.IsAvailable,
                UpdatedAt = svc.UpdatedAt,
                Message = "Xóa combo thành công"
            };
        }

        // ====== VALIDATIONS ======
        private void ValidateCreate(CreateServiceRequest r)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 255)
                errors["name"] = new ValidationError { Msg = "Tên combo là bắt buộc và tối đa 255 ký tự", Path = "name" };

            if (string.IsNullOrWhiteSpace(r.Code) || r.Code.Trim().Length > 50 || !Regex.IsMatch(r.Code, @"^[A-Z0-9_]+$", RegexOptions.IgnoreCase))
                errors["code"] = new ValidationError { Msg = "Mã combo chỉ gồm chữ cái, số, gạch dưới và tối đa 50 ký tự", Path = "code" };

            if (r.Price < 0)
                errors["price"] = new ValidationError { Msg = "Giá phải >= 0", Path = "price" };

            if (!string.IsNullOrWhiteSpace(r.ImageUrl) && !IsValidImageUrl(r.ImageUrl))
                errors["imageUrl"] = new ValidationError { Msg = "ImageUrl phải là URL http/https hợp lệ và là định dạng ảnh (jpg, jpeg, png, webp, svg)", Path = "imageUrl" };

            if (errors.Any()) throw new ValidationException(errors);
        }

        private void ValidateUpdate(UpdateServiceRequest r)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 255)
                errors["name"] = new ValidationError { Msg = "Tên combo là bắt buộc và tối đa 255 ký tự", Path = "name" };

            if (r.Price < 0)
                errors["price"] = new ValidationError { Msg = "Giá phải >= 0", Path = "price" };

            if (!string.IsNullOrWhiteSpace(r.ImageUrl) && !IsValidImageUrl(r.ImageUrl))
                errors["imageUrl"] = new ValidationError { Msg = "ImageUrl phải là URL http/https hợp lệ và là định dạng ảnh (jpg, jpeg, png, webp, svg)", Path = "imageUrl" };

            if (errors.Any()) throw new ValidationException(errors);
        }

        private void ValidateListQuery(GetServicesQuery q)
        {
            var errors = new Dictionary<string, ValidationError>();
            if (q.Page < 1) errors["page"] = new ValidationError { Msg = "Số trang phải lớn hơn 0", Path = "page" };
            if (q.Limit < 1 || q.Limit > 100) errors["limit"] = new ValidationError { Msg = "Số lượng mỗi trang phải từ 1 đến 100", Path = "limit" };
            if (errors.Any()) throw new ValidationException(errors);
        }

        private async Task ValidateUniqueCodeAsync(int partnerId, string code)
        {
            var exists = await _context.Services
                .AnyAsync(x => x.PartnerId == partnerId && x.Code.ToUpper() == code.Trim().ToUpper());
            if (exists)
                throw new ConflictException("code", "Mã combo đã tồn tại trong hệ thống của bạn");
        }

        private async Task ValidatePartnerAccessAsync(int partnerId, int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.UserType == "Partner");
            if (user == null) throw new UnauthorizedException("Chỉ tài khoản Partner mới được sử dụng chức năng này");

            var partner = await _context.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.UserId == userId && p.Status == "approved");
            if (partner == null) throw new UnauthorizedException("Partner không tồn tại hoặc không thuộc quyền quản lý của bạn");
            if (!partner.IsActive) throw new UnauthorizedException("Tài khoản partner đã bị vô hiệu hóa");
        }

        private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static bool IsValidImageUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
            if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return false;
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
            var ext = Path.GetExtension(u.AbsolutePath).ToLower();
            return !string.IsNullOrEmpty(ext) && allowed.Contains(ext) && u.AbsolutePath.Length <= 255;
        }

        private static ServiceResponse Map(Service s) => new()
        {
            ServiceId = s.ServiceId,
            PartnerId = s.PartnerId,
            Name = s.ServiceName,
            Code = s.Code,
            Price = s.Price,
            IsAvailable = s.IsAvailable,
            Description = s.Description,
            ImageUrl = s.ImageUrl,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
