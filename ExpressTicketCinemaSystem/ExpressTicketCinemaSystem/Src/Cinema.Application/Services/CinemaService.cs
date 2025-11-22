using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class CinemaService : ICinemaService
    {
        private readonly CinemaDbCoreContext _context;

        public CinemaService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task<CinemaResponse> CreateCinemaAsync(CreateCinemaRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateCreateCinemaRequest(request);
            await ValidatePartnerAccessAsync(partnerId, userId);
            await ValidateCinemaCodeUniqueAsync(request.Code, partnerId);

            // ==================== BUSINESS LOGIC SECTION ====================
            await ValidateCinemaBusinessRulesAsync(request, partnerId);
            // SỬA Ở ĐÂY: Sử dụng fully qualified name hoặc alias nếu cần
            var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) 
                ? null 
                : NormalizePhoneNumber(Regex.Replace(request.Phone.Trim(), @"\s+", ""));
            
            var cinema = new Infrastructure.Models.Cinema
            {
                PartnerId = partnerId,
                CinemaName = request.CinemaName.Trim(),
                Address = request.Address.Trim(),
                Phone = normalizedPhone,
                Code = request.Code.Trim().ToUpper(),
                City = request.City.Trim(),
                District = request.District.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Email = request.Email?.Trim(),
                LogoUrl = request.LogoUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();

            return await MapToCinemaResponseAsync(cinema);
        }

        public async Task<CinemaResponse> GetCinemaByIdAsync(int cinemaId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            return await MapToCinemaResponseAsync(cinema);
        }

        public async Task<PaginatedCinemasResponse> GetCinemasAsync(int partnerId, int userId, int page = 1, int limit = 10,
            string? city = null, string? district = null, bool? isActive = null, string? search = null,
            string? sortBy = "cinema_name", string? sortOrder = "asc")
        {
            // ==================== VALIDATION SECTION ====================
            ValidateGetCinemasRequest(page, limit);
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var query = _context.Cinemas
                .Where(c => c.PartnerId == partnerId)
                .AsQueryable();

            // Apply filters
            query = ApplyCinemaFilters(query, city, district, isActive, search);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplyCinemaSorting(query, sortBy, sortOrder);

            // Apply pagination
            var cinemas = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var cinemaResponses = new List<CinemaResponse>();
            foreach (var cinema in cinemas)
            {
                cinemaResponses.Add(await MapToCinemaResponseAsync(cinema));
            }

            var pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedCinemasResponse
            {
                Cinemas = cinemaResponses,
                Pagination = pagination
            };
        }

        public async Task<CinemaResponse> UpdateCinemaAsync(int cinemaId, UpdateCinemaRequest request, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateUpdateCinemaRequest(request);
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================

            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            await ValidateCinemaBusinessRulesForUpdateAsync(cinemaId, request, partnerId);

            // Validate không thể deactivate nếu còn phòng active
            if (!request.IsActive && cinema.IsActive == true)
            {
                var activeScreensCount = await _context.Screens
                    .CountAsync(s => s.CinemaId == cinemaId && s.IsActive);

                if (activeScreensCount > 0)
                {
                    throw new ValidationException(new Dictionary<string, ValidationError>
                    {
                        ["isActive"] = new ValidationError
                        {
                            Msg = $"Không thể vô hiệu hóa rạp đang có {activeScreensCount} phòng chiếu đang hoạt động",
                            Path = "isActive"
                        }
                    });
                }
            }

            cinema.CinemaName = request.CinemaName.Trim();
            cinema.Address = request.Address.Trim();
            cinema.Phone = string.IsNullOrWhiteSpace(request.Phone) 
                ? null 
                : NormalizePhoneNumber(Regex.Replace(request.Phone.Trim(), @"\s+", ""));
            cinema.City = request.City.Trim();
            cinema.District = request.District.Trim();
            cinema.Latitude = request.Latitude;
            cinema.Longitude = request.Longitude;
            cinema.Email = request.Email?.Trim();
            cinema.LogoUrl = request.LogoUrl;
            cinema.IsActive = request.IsActive;
            cinema.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToCinemaResponseAsync(cinema);
        }
        private async Task ValidateCinemaBusinessRulesForUpdateAsync(int cinemaId, UpdateCinemaRequest request, int partnerId)
        {
            var errors = new Dictionary<string, ValidationError>();

            var existingName = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                         c.CinemaId != cinemaId && 
                                         c.CinemaName.Trim().ToLower() == request.CinemaName.Trim().ToLower());

            if (existingName != null)
            {
                errors["cinemaName"] = new ValidationError
                {
                    Msg = "Tên rạp đã tồn tại trong hệ thống của bạn",
                    Path = "cinemaName"
                };
            }

            var existingDistrict = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                         c.CinemaId != cinemaId && 
                                         c.District.Trim().ToLower() == request.District.Trim().ToLower() &&
                                         c.City.Trim().ToLower() == request.City.Trim().ToLower());

            if (existingDistrict != null)
            {
                errors["district"] = new ValidationError
                {
                    Msg = $"Đã có rạp khác trong {request.District}, {request.City}. Mỗi quận/huyện chỉ được có một rạp.",
                    Path = "district"
                };
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var phone = request.Phone.Trim();
                var cleanPhone = Regex.Replace(phone, @"\s+", ""); // Bỏ tất cả khoảng trắng
                
                if (!ValidateCinemaPhoneNumber(cleanPhone))
                {
                    errors["phone"] = new ValidationError
                    {
                        Msg = "Số điện thoại không đúng định dạng Việt Nam (ví dụ: 0912345678, +84912345678, +84 1900 6017)",
                        Path = "phone"
                    };
                }
                else
                {
                    // Lưu số điện thoại đã được clean
                    var normalizedPhone = NormalizePhoneNumber(cleanPhone);
                    
                    var existingPhone = await _context.Cinemas
                        .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                                 c.CinemaId != cinemaId && 
                                                 c.Phone == normalizedPhone);

                    if (existingPhone != null)
                    {
                        errors["phone"] = new ValidationError
                        {
                            Msg = "Số điện thoại đã được sử dụng cho rạp khác",
                            Path = "phone"
                        };
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                if (!Uri.TryCreate(request.LogoUrl, UriKind.Absolute, out var uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    errors["logoUrl"] = new ValidationError
                    {
                        Msg = "Logo URL không hợp lệ. Phải là URL đầy đủ (http:// hoặc https://)",
                        Path = "logoUrl"
                    };
                }
                else
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                    var extension = Path.GetExtension(uriResult.AbsolutePath).ToLower();

                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        errors["logoUrl"] = new ValidationError
                        {
                            Msg = "Logo phải là file ảnh (jpg, jpeg, png, webp, svg)",
                            Path = "logoUrl"
                        };
                    }
                    if (uriResult.AbsolutePath.Length > 255)
                    {
                        errors["logoUrl"] = new ValidationError
                        {
                            Msg = "Đường dẫn logo quá dài",
                            Path = "logoUrl"
                        };
                    }
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        public async Task<CinemaActionResponse> DeleteCinemaAsync(int cinemaId, int partnerId, int userId)
        {
            // ==================== VALIDATION SECTION ====================
            await ValidatePartnerAccessAsync(partnerId, userId);

            // ==================== BUSINESS LOGIC SECTION ====================
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp với ID này hoặc không thuộc quyền quản lý của bạn");
            }

            // Validate không thể xóa nếu còn phòng
            var screensCount = await _context.Screens
                .CountAsync(s => s.CinemaId == cinemaId);

            if (screensCount > 0)
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["delete"] = new ValidationError
                    {
                        Msg = $"Không thể xóa rạp đang có {screensCount} phòng chiếu",
                        Path = "cinemaId"
                    }
                });
            }

            // SOFT DELETE - Chỉ cập nhật IsActive = false
            cinema.IsActive = false;
            cinema.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CinemaActionResponse
            {
                CinemaId = cinema.CinemaId,
                CinemaName = cinema.CinemaName,
                Message = "Xóa rạp thành công",
                IsActive = cinema.IsActive ?? false,
                UpdatedAt = cinema.UpdatedAt ?? DateTime.UtcNow,
            };
        }

        // ==================== PRIVATE METHODS ====================
        private async Task<CinemaResponse> MapToCinemaResponseAsync(Infrastructure.Models.Cinema cinema)
        {
            var totalScreens = await _context.Screens
                .CountAsync(s => s.CinemaId == cinema.CinemaId);

            var activeScreens = await _context.Screens
                .CountAsync(s => s.CinemaId == cinema.CinemaId && s.IsActive);

            return new CinemaResponse
            {
                CinemaId = cinema.CinemaId,
                PartnerId = cinema.PartnerId,
                CinemaName = cinema.CinemaName,
                Address = cinema.Address,
                Phone = cinema.Phone,
                Code = cinema.Code,
                City = cinema.City,
                District = cinema.District,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude,
                Email = cinema.Email,
                IsActive = cinema.IsActive ?? true,
                LogoUrl = cinema.LogoUrl,
                CreatedAt = cinema.CreatedAt,
                UpdatedAt = cinema.UpdatedAt ?? DateTime.UtcNow,
                TotalScreens = totalScreens,
                ActiveScreens = activeScreens
            };
        }

        private void ValidateCreateCinemaRequest(CreateCinemaRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.CinemaName))
                errors["cinemaName"] = new ValidationError { Msg = "Tên rạp là bắt buộc", Path = "cinemaName" };
            else if (request.CinemaName.Trim().Length > 255)
                errors["cinemaName"] = new ValidationError { Msg = "Tên rạp không được vượt quá 255 ký tự", Path = "cinemaName" };

            if (string.IsNullOrWhiteSpace(request.Address))
                errors["address"] = new ValidationError { Msg = "Địa chỉ là bắt buộc", Path = "address" };
            else if (request.Address.Trim().Length > 500)
                errors["address"] = new ValidationError { Msg = "Địa chỉ không được vượt quá 500 ký tự", Path = "address" };

            if (string.IsNullOrWhiteSpace(request.Code))
                errors["code"] = new ValidationError { Msg = "Mã rạp là bắt buộc", Path = "code" };
            else if (request.Code.Trim().Length > 50)
                errors["code"] = new ValidationError { Msg = "Mã rạp không được vượt quá 50 ký tự", Path = "code" };
            else if (!Regex.IsMatch(request.Code, @"^[A-Z0-9_]+$", RegexOptions.IgnoreCase))
                errors["code"] = new ValidationError { Msg = "Mã rạp chỉ được chứa chữ cái, số và dấu gạch dưới", Path = "code" };

            if (string.IsNullOrWhiteSpace(request.City))
                errors["city"] = new ValidationError { Msg = "Thành phố là bắt buộc", Path = "city" };
            else if (request.City.Trim().Length > 100)
                errors["city"] = new ValidationError { Msg = "Thành phố không được vượt quá 100 ký tự", Path = "city" };

            if (string.IsNullOrWhiteSpace(request.District))
                errors["district"] = new ValidationError { Msg = "Quận/huyện là bắt buộc", Path = "district" };
            else if (request.District.Trim().Length > 100)
                errors["district"] = new ValidationError { Msg = "Quận/huyện không được vượt quá 100 ký tự", Path = "district" };

            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Length > 20)
                errors["phone"] = new ValidationError { Msg = "Số điện thoại không được vượt quá 20 ký tự", Path = "phone" };

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Length > 255)
                errors["email"] = new ValidationError { Msg = "Email không được vượt quá 255 ký tự", Path = "email" };

            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Length > 20)
                errors["phone"] = new ValidationError
                {
                    Msg = "Số điện thoại không được vượt quá 20 ký tự",
                    Path = "phone"
                };

            if (!string.IsNullOrWhiteSpace(request.LogoUrl) && request.LogoUrl.Length > 500)
                errors["logoUrl"] = new ValidationError
                {
                    Msg = "Logo URL không được vượt quá 500 ký tự",
                    Path = "logoUrl"
                };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateUpdateCinemaRequest(UpdateCinemaRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.CinemaName))
                errors["cinemaName"] = new ValidationError { Msg = "Tên rạp là bắt buộc", Path = "cinemaName" };
            else if (request.CinemaName.Trim().Length > 255)
                errors["cinemaName"] = new ValidationError { Msg = "Tên rạp không được vượt quá 255 ký tự", Path = "cinemaName" };

            if (string.IsNullOrWhiteSpace(request.Address))
                errors["address"] = new ValidationError { Msg = "Địa chỉ là bắt buộc", Path = "address" };
            else if (request.Address.Trim().Length > 500)
                errors["address"] = new ValidationError { Msg = "Địa chỉ không được vượt quá 500 ký tự", Path = "address" };

            if (string.IsNullOrWhiteSpace(request.City))
                errors["city"] = new ValidationError { Msg = "Thành phố là bắt buộc", Path = "city" };
            else if (request.City.Trim().Length > 100)
                errors["city"] = new ValidationError { Msg = "Thành phố không được vượt quá 100 ký tự", Path = "city" };

            if (string.IsNullOrWhiteSpace(request.District))
                errors["district"] = new ValidationError { Msg = "Quận/huyện là bắt buộc", Path = "district" };
            else if (request.District.Trim().Length > 100)
                errors["district"] = new ValidationError { Msg = "Quận/huyện không được vượt quá 100 ký tự", Path = "district" };

            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Length > 20)
                errors["phone"] = new ValidationError { Msg = "Số điện thoại không được vượt quá 20 ký tự", Path = "phone" };

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Length > 255)
                errors["email"] = new ValidationError { Msg = "Email không được vượt quá 255 ký tự", Path = "email" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateGetCinemasRequest(int page, int limit)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (page < 1)
                errors["page"] = new ValidationError { Msg = "Số trang phải lớn hơn 0", Path = "page" };

            if (limit < 1 || limit > 100)
                errors["limit"] = new ValidationError { Msg = "Số lượng mỗi trang phải từ 1 đến 100", Path = "limit" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidatePartnerAccessAsync(int partnerId, int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.UserType == "Partner");

            if (user == null)
            {
                throw new UnauthorizedException("Chỉ tài khoản Partner mới được sử dụng chức năng này");
            }

            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.UserId == userId && p.Status == "approved");

            if (partner == null)
            {
                throw new UnauthorizedException("Partner không tồn tại hoặc không thuộc quyền quản lý của bạn");
            }

            if (!partner.IsActive)
            {
                throw new UnauthorizedException("Tài khoản partner đã bị vô hiệu hóa");
            }
        }

        private async Task ValidateCinemaCodeUniqueAsync(string code, int partnerId)
        {
            var existingCinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.PartnerId == partnerId && c.Code.ToUpper() == code.Trim().ToUpper());

            if (existingCinema != null)
            {
                throw new ConflictException("code", "Mã rạp đã tồn tại trong hệ thống của bạn");
            }
        }
        private async Task ValidateCinemaBusinessRulesAsync(CreateCinemaRequest request, int partnerId)
        {
            var errors = new Dictionary<string, ValidationError>();

            var existingName = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                         c.CinemaName.Trim().ToLower() == request.CinemaName.Trim().ToLower());

            if (existingName != null)
            {
                errors["cinemaName"] = new ValidationError
                {
                    Msg = "Tên rạp đã tồn tại trong hệ thống của bạn",
                    Path = "cinemaName"
                };
            }

            var existingDistrict = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                         c.District.Trim().ToLower() == request.District.Trim().ToLower() &&
                                         c.City.Trim().ToLower() == request.City.Trim().ToLower());

            if (existingDistrict != null)
            {
                errors["district"] = new ValidationError
                {
                    Msg = $"Đã có rạp khác trong {request.District}, {request.City}. Mỗi quận/huyện chỉ được có một rạp.",
                    Path = "district"
                };
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var phone = request.Phone.Trim();
                var cleanPhone = Regex.Replace(phone, @"\s+", ""); // Bỏ tất cả khoảng trắng
                
                if (!ValidateCinemaPhoneNumber(cleanPhone))
                {
                    errors["phone"] = new ValidationError
                    {
                        Msg = "Số điện thoại không đúng định dạng Việt Nam (ví dụ: 0912345678, +84912345678, +84 1900 6017)",
                        Path = "phone"
                    };
                }
                else
                {
                    // Lưu số điện thoại đã được clean
                    var normalizedPhone = NormalizePhoneNumber(cleanPhone);
                    
                    var existingPhone = await _context.Cinemas
                        .FirstOrDefaultAsync(c => c.PartnerId == partnerId &&
                                                 c.Phone == normalizedPhone);

                    if (existingPhone != null)
                    {
                        errors["phone"] = new ValidationError
                        {
                            Msg = "Số điện thoại đã được sử dụng cho rạp khác",
                            Path = "phone"
                        };
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                if (!Uri.TryCreate(request.LogoUrl, UriKind.Absolute, out var uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    errors["logoUrl"] = new ValidationError
                    {
                        Msg = "Logo URL không hợp lệ. Phải là URL đầy đủ (http:// hoặc https://)",
                        Path = "logoUrl"
                    };
                }
                else
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                    var extension = Path.GetExtension(uriResult.AbsolutePath).ToLower();

                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        errors["logoUrl"] = new ValidationError
                        {
                            Msg = "Logo phải là file ảnh (jpg, jpeg, png, webp, svg)",
                            Path = "logoUrl"
                        };
                    }
                    if (uriResult.AbsolutePath.Length > 255)
                    {
                        errors["logoUrl"] = new ValidationError
                        {
                            Msg = "Đường dẫn logo quá dài",
                            Path = "logoUrl"
                        };
                    }
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private IQueryable<Infrastructure.Models.Cinema> ApplyCinemaFilters(IQueryable<Infrastructure.Models.Cinema> query, string? city, string? district, bool? isActive, string? search)
        {
            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(c => c.City.ToLower().Contains(city.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(district))
            {
                query = query.Where(c => c.District.ToLower().Contains(district.Trim().ToLower()));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CinemaName.ToLower().Contains(searchTerm) ||
                    c.Code.ToLower().Contains(searchTerm) ||
                    c.Address.ToLower().Contains(searchTerm)
                );
            }

            return query;
        }

        private IQueryable<Infrastructure.Models.Cinema> ApplyCinemaSorting(IQueryable<Infrastructure.Models.Cinema> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "cinema_name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "code" => isAscending ? query.OrderBy(c => c.Code) : query.OrderByDescending(c => c.Code),
                "city" => isAscending ? query.OrderBy(c => c.City) : query.OrderByDescending(c => c.City),
                "district" => isAscending ? query.OrderBy(c => c.District) : query.OrderByDescending(c => c.District),
                "created_at" => isAscending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
                "updated_at" => isAscending ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
                _ => isAscending ? query.OrderBy(c => c.CinemaName) : query.OrderByDescending(c => c.CinemaName)
            };
        }

        /// <summary>
        /// Validate số điện thoại cho rạp chiếu phim
        /// Hỗ trợ: 0xxxxxxxxx (10 số), +84xxxxxxxxx (9 số sau +84), +84xxxxxxxx (8 số sau +84 phải bắt đầu bằng 1900)
        /// </summary>
        private bool ValidateCinemaPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Bỏ tất cả khoảng trắng
            var cleanPhone = Regex.Replace(phone, @"\s+", "");

            // Nếu bắt đầu bằng +84
            if (cleanPhone.StartsWith("+84"))
            {
                var numberPart = cleanPhone.Substring(3); // Lấy phần sau +84

                // Nếu 8 số: phải bắt đầu bằng 1900
                if (numberPart.Length == 8)
                {
                    return numberPart.StartsWith("1900") && Regex.IsMatch(numberPart, @"^1900[0-9]{4}$");
                }
                // Nếu 9 số: kiểm tra đầu số hợp lệ
                else if (numberPart.Length == 9)
                {
                    return Regex.IsMatch(numberPart, @"^(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$");
                }
                else
                {
                    return false;
                }
            }
            // Nếu bắt đầu bằng 0
            else if (cleanPhone.StartsWith("0"))
            {
                // Phải có đúng 10 số và đầu số hợp lệ
                if (cleanPhone.Length == 10)
                {
                    return Regex.IsMatch(cleanPhone, @"^0(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$");
                }
                else
                {
                    return false;
                }
            }
            // Nếu bắt đầu bằng 84 (không có dấu +)
            else if (cleanPhone.StartsWith("84"))
            {
                var numberPart = cleanPhone.Substring(2); // Lấy phần sau 84

                // Nếu 8 số: phải bắt đầu bằng 1900
                if (numberPart.Length == 8)
                {
                    return numberPart.StartsWith("1900") && Regex.IsMatch(numberPart, @"^1900[0-9]{4}$");
                }
                // Nếu 9 số: kiểm tra đầu số hợp lệ
                else if (numberPart.Length == 9)
                {
                    return Regex.IsMatch(numberPart, @"^(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$");
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Chuẩn hóa số điện thoại về dạng 0xxxxxxxxx hoặc +84xxxxxxxxx
        /// </summary>
        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Bỏ tất cả khoảng trắng
            var cleanPhone = Regex.Replace(phone.Trim(), @"\s+", "");

            // Nếu bắt đầu bằng +84
            if (cleanPhone.StartsWith("+84"))
            {
                return cleanPhone; // Giữ nguyên +84
            }
            // Nếu bắt đầu bằng 84 (không có dấu +)
            else if (cleanPhone.StartsWith("84") && cleanPhone.Length >= 11)
            {
                return "+" + cleanPhone; // Thêm dấu +
            }
            // Nếu bắt đầu bằng 0
            else if (cleanPhone.StartsWith("0"))
            {
                return cleanPhone; // Giữ nguyên
            }

            return cleanPhone;
        }
    }
}