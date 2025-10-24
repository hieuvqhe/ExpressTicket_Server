using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class PartnerService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;

        public PartnerService(
            CinemaDbCoreContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task<PartnerRegisterResponse> RegisterPartnerAsync(PartnerRegisterRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateRequiredFields(request);
            ValidatePasswordConfirmation(request.Password, request.ConfirmPassword);
            await ValidateEmailAsync(request.Email);
            ValidatePassword(request.Password);
            ValidatePhoneNumber(request.Phone);
            await ValidateBusinessInfoAsync(request.PartnerName, request.TaxCode);
            ValidateCommissionRate(request.CommissionRate);
            ValidateDocuments(request);
            ValidateTheaterPhotos(request.TheaterPhotosUrls);

            // ==================== BUSINESS LOGIC SECTION ====================

            var user = new User
            {
                Email = request.Email,
                Password = _passwordHasher.HashPassword(null, request.Password),
                Phone = request.Phone,
                UserType = "Partner",
                Fullname = request.FullName,
                IsActive = false, 
                EmailConfirmed = false, 
                Username = request.Email, 
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); 

            var partner = new Partner
            {
                UserId = user.UserId,
                ManagerId = null, 
                PartnerName = request.PartnerName,
                TaxCode = request.TaxCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                CommissionRate = request.CommissionRate,
                IsActive = false,
                BusinessRegistrationCertificateUrl = request.BusinessRegistrationCertificateUrl,
                TaxRegistrationCertificateUrl = request.TaxRegistrationCertificateUrl,
                IdentityCardUrl = request.IdentityCardUrl,
                TheaterPhotosUrl = string.Join(";", request.TheaterPhotosUrls), 
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();

            await _emailService.SendPartnerRegistrationConfirmationAsync(
                user.Email,
                user.Fullname,
                partner.PartnerName
            );

            return new PartnerRegisterResponse
            {
                Message = "Đăng ký đối tác thành công. Hồ sơ của bạn đang chờ xét duyệt.",
                PartnerId = partner.PartnerId,
                Status = partner.Status,
                CreatedAt = partner.CreatedAt
            };
        }


        private void ValidateRequiredFields(PartnerRegisterRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["email"] = new ValidationError { Msg = "Email là bắt buộc", Path = "email" };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new ValidationError { Msg = "Mật khẩu là bắt buộc", Path = "password" };

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                errors["confirmPassword"] = new ValidationError { Msg = "Xác nhận mật khẩu là bắt buộc", Path = "confirmPassword" };

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new ValidationError { Msg = "Họ và tên là bắt buộc", Path = "fullName" };

            if (string.IsNullOrWhiteSpace(request.Phone))
                errors["phone"] = new ValidationError { Msg = "Số điện thoại là bắt buộc", Path = "phone" };

            if (string.IsNullOrWhiteSpace(request.PartnerName))
                errors["partnerName"] = new ValidationError { Msg = "Tên doanh nghiệp là bắt buộc", Path = "partnerName" };

            if (string.IsNullOrWhiteSpace(request.TaxCode))
                errors["taxCode"] = new ValidationError { Msg = "Mã số thuế là bắt buộc", Path = "taxCode" };

            if (string.IsNullOrWhiteSpace(request.Address))
                errors["address"] = new ValidationError { Msg = "Địa chỉ kinh doanh là bắt buộc", Path = "address" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateEmailAsync(string email)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.TryAdd("email", new ValidationError { Msg = "Định dạng email không hợp lệ", Path = "email" });

            var blockedDomains = new[] { "tempmail.com", "throwaway.com", "10minutemail.com" };
            var emailDomain = email.Split('@').Last().ToLower();
            if (blockedDomains.Any(domain => emailDomain.Contains(domain)))
                errors.TryAdd("email", new ValidationError { Msg = "Email tạm thời không được phép sử dụng", Path = "email" });

            if (errors.Any())
                throw new ValidationException(errors);

            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new ConflictException("email", "Email đã tồn tại trong hệ thống");
        }

        private void ValidatePassword(string password)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (password.Length < 6)
                errors.TryAdd("password", new ValidationError { Msg = "Mật khẩu phải có ít nhất 6 ký tự", Path = "password" });
            else if (password.Length > 12) 
                errors.TryAdd("password", new ValidationError { Msg = "Mật khẩu không được vượt quá 12 ký tự", Path = "password" });

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.TryAdd("password_lowercase", new ValidationError { Msg = "Mật khẩu phải chứa ít nhất một chữ cái thường", Path = "password" });

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.TryAdd("password_uppercase", new ValidationError { Msg = "Mật khẩu phải chứa ít nhất một chữ cái hoa", Path = "password" });

            if (!Regex.IsMatch(password, @"\d"))
                errors.TryAdd("password_digit", new ValidationError { Msg = "Mật khẩu phải chứa ít nhất một chữ số", Path = "password" });

            if (!Regex.IsMatch(password, @"[@$!%*?&]"))
                errors.TryAdd("password_special", new ValidationError { Msg = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (@$!%*?&)", Path = "password" });

            var commonPasswords = new[] { "Password123!", "Admin123!", "Partner123!" };
            if (commonPasswords.Contains(password))
                errors.TryAdd("password_common", new ValidationError { Msg = "Mật khẩu này quá phổ biến. Vui lòng chọn mật khẩu khác", Path = "password" });

            if (errors.Any())
                throw new ValidationException(errors); 
        }

        private void ValidatePhoneNumber(string phone)
        {
            var errors = new Dictionary<string, ValidationError>();
            var cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");

            if (!Regex.IsMatch(cleanPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
                errors.TryAdd("phone", new ValidationError { Msg = "Định dạng số điện thoại không hợp lệ. Vui lòng sử dụng số điện thoại Việt Nam hợp lệ", Path = "phone" });

            if (errors.Any())
                throw new ValidationException(errors); 
        }

        private async Task ValidateBusinessInfoAsync(string partnerName, string taxCode)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (partnerName.Length < 2)
                errors.TryAdd("partnerName", new ValidationError { Msg = "Tên doanh nghiệp phải có ít nhất 2 ký tự", Path = "partnerName" });
            else if (partnerName.Length > 255)
                errors.TryAdd("partnerName", new ValidationError { Msg = "Tên doanh nghiệp không được vượt quá 255 ký tự", Path = "partnerName" });

            if (taxCode.Length != 10 && taxCode.Length != 13)
                errors.TryAdd("taxCode", new ValidationError { Msg = "Mã số thuế phải có 10 hoặc 13 chữ số", Path = "taxCode" });
            else if (!Regex.IsMatch(taxCode, @"^\d+$")) 
                errors.TryAdd("taxCode", new ValidationError { Msg = "Mã số thuế chỉ được chứa chữ số", Path = "taxCode" });

            if (errors.Any())
                throw new ValidationException(errors); 

            if (await _context.Partners.AnyAsync(p => p.PartnerName == partnerName))
                throw new ConflictException("partnerName", "Tên doanh nghiệp đã tồn tại");

            if (await _context.Partners.AnyAsync(p => p.TaxCode == taxCode))
                throw new ConflictException("taxCode", "Mã số thuế đã được đăng ký");
        }

        private void ValidateCommissionRate(decimal commissionRate)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (commissionRate < 0)
                errors.TryAdd("commissionRate", new ValidationError { Msg = "Tỷ lệ hoa hồng không thể âm", Path = "commissionRate" });
            else if (commissionRate > 50)
                errors.TryAdd("commissionRate", new ValidationError { Msg = "Tỷ lệ hoa hồng không thể vượt quá 50%", Path = "commissionRate" });

            if (decimal.Round(commissionRate, 2) != commissionRate)
                errors.TryAdd("commissionRate_decimal", new ValidationError { Msg = "Tỷ lệ hoa hồng chỉ được có tối đa 2 chữ số thập phân", Path = "commissionRate" });

            if (errors.Any())
                throw new ValidationException(errors); 
        }

        private void ValidateDocuments(PartnerRegisterRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.BusinessRegistrationCertificateUrl))
                errors.TryAdd("businessRegistrationCertificateUrl", new ValidationError { Msg = "Giấy chứng nhận đăng ký doanh nghiệp là bắt buộc", Path = "businessRegistrationCertificateUrl" });

            if (string.IsNullOrWhiteSpace(request.TaxRegistrationCertificateUrl))
                errors.TryAdd("taxRegistrationCertificateUrl", new ValidationError { Msg = "Giấy chứng nhận đăng ký thuế là bắt buộc", Path = "taxRegistrationCertificateUrl" });

            if (string.IsNullOrWhiteSpace(request.IdentityCardUrl))
                errors.TryAdd("identityCardUrl", new ValidationError { Msg = "Ảnh CMND/CCCD là bắt buộc", Path = "identityCardUrl" });

            var urlPattern = @"^https?://.+\.(jpg|jpeg|png|pdf|doc|docx)$";
            if (!string.IsNullOrWhiteSpace(request.BusinessRegistrationCertificateUrl) && !Regex.IsMatch(request.BusinessRegistrationCertificateUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("businessRegistrationCertificateUrl_pattern", new ValidationError { Msg = "Giấy ĐKDN phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "businessRegistrationCertificateUrl" });

            if (!string.IsNullOrWhiteSpace(request.TaxRegistrationCertificateUrl) && !Regex.IsMatch(request.TaxRegistrationCertificateUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("taxRegistrationCertificateUrl_pattern", new ValidationError { Msg = "Giấy ĐKT phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "taxRegistrationCertificateUrl" });

            if (!string.IsNullOrWhiteSpace(request.IdentityCardUrl) && !Regex.IsMatch(request.IdentityCardUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("identityCardUrl_pattern", new ValidationError { Msg = "Ảnh CMND/CCCD phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "identityCardUrl" });

            if (errors.Any())
                throw new ValidationException(errors); 
        }

        private void ValidateTheaterPhotos(List<string> theaterPhotosUrls)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (theaterPhotosUrls == null || !theaterPhotosUrls.Any())
                errors.TryAdd("theaterPhotosUrls", new ValidationError { Msg = "Cần ít nhất một ảnh rạp chiếu phim", Path = "theaterPhotosUrls" });
            else if (theaterPhotosUrls.Count > 10)
                errors.TryAdd("theaterPhotosUrls", new ValidationError { Msg = "Chỉ được phép tối đa 10 ảnh rạp chiếu phim", Path = "theaterPhotosUrls" });
            else
            {
                var urlPattern = @"^https?://.+\.(jpg|jpeg|png)$";
                foreach (var photoUrl in theaterPhotosUrls)
                {
                    if (string.IsNullOrWhiteSpace(photoUrl))
                    {
                        errors.TryAdd("theaterPhotosUrls_empty", new ValidationError { Msg = "URL ảnh rạp không được để trống", Path = "theaterPhotosUrls" });
                        break;
                    }
                    if (!Regex.IsMatch(photoUrl, urlPattern, RegexOptions.IgnoreCase))
                    {
                        errors.TryAdd("theaterPhotosUrls_pattern", new ValidationError { Msg = $"URL ảnh rạp không hợp lệ: {photoUrl}. Phải là URL ảnh (jpg, jpeg, png)", Path = "theaterPhotosUrls" });
                        break;
                    }
                }

                if (theaterPhotosUrls.Distinct().Count() != theaterPhotosUrls.Count)
                    errors.TryAdd("theaterPhotosUrls_duplicate", new ValidationError { Msg = "Không được phép có URL ảnh trùng lặp", Path = "theaterPhotosUrls" });
            }

            if (errors.Any())
                throw new ValidationException(errors); 
        }
        private void ValidatePasswordConfirmation(string password, string confirmPassword)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (password != confirmPassword)
            {
                errors["confirmPassword"] = new ValidationError
                {
                    Msg = "Mật khẩu và xác nhận mật khẩu không khớp",
                    Path = "confirmPassword"
                };
                throw new ValidationException(errors);
            }
        }
        public async Task<PartnerProfileResponse> GetPartnerProfileAsync(int userId)
        {
            var partner = await _context.Partners
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ đối tác cho người dùng này.");
            }

            if (partner.Status != "approved")
            {
                throw new UnauthorizedException($"Tài khoản đối tác của bạn chưa được duyệt (trạng thái: {partner.Status}).");
            }

            var theaterPhotos = string.IsNullOrEmpty(partner.TheaterPhotosUrl)
                ? new List<string>()
                : partner.TheaterPhotosUrl.Split(';').ToList();

            return new PartnerProfileResponse
            {
                UserId = partner.User.UserId,
                Email = partner.User.Email,
                Phone = partner.User.Phone,
                FullName = partner.User.Fullname,
                AvatarUrl = partner.User.AvatarUrl,

                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Address = partner.Address,
                CommissionRate = partner.CommissionRate,

                BusinessRegistrationCertificateUrl = partner.BusinessRegistrationCertificateUrl ?? "",
                TaxRegistrationCertificateUrl = partner.TaxRegistrationCertificateUrl ?? "",
                IdentityCardUrl = partner.IdentityCardUrl ?? "",
                TheaterPhotosUrls = theaterPhotos,

                Status = partner.Status,
                CreatedAt = partner.CreatedAt,
                UpdatedAt = partner.UpdatedAt
            };
        }
        public async Task<PartnerProfileResponse> UpdatePartnerProfileAsync(int userId, PartnerUpdateRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateUpdateRequiredFields(request);
            ValidatePhoneNumber(request.Phone);
            await ValidateBusinessInfoForUpdateAsync(userId, request.PartnerName, request.TaxCode);
            ValidateCommissionRate(request.CommissionRate);
            ValidateDocuments(request);
            ValidateTheaterPhotos(request.TheaterPhotosUrls);

            // ==================== BUSINESS LOGIC SECTION ====================

            var partner = await _context.Partners
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ đối tác cho người dùng này.");
            }

            if (partner.Status != "approved")
            {
                throw new UnauthorizedException($"Tài khoản đối tác của bạn chưa được duyệt (trạng thái: {partner.Status}).");
            }

            partner.User.Fullname = request.FullName;
            partner.User.Phone = request.Phone;
            // partner.User.UpdatedAt = DateTime.UtcNow;

            partner.PartnerName = request.PartnerName;
            partner.TaxCode = request.TaxCode;
            partner.Address = request.Address;
            partner.CommissionRate = request.CommissionRate;
            partner.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.BusinessRegistrationCertificateUrl))
                partner.BusinessRegistrationCertificateUrl = request.BusinessRegistrationCertificateUrl;

            if (!string.IsNullOrWhiteSpace(request.TaxRegistrationCertificateUrl))
                partner.TaxRegistrationCertificateUrl = request.TaxRegistrationCertificateUrl;

            if (!string.IsNullOrWhiteSpace(request.IdentityCardUrl))
                partner.IdentityCardUrl = request.IdentityCardUrl;

            if (request.TheaterPhotosUrls != null && request.TheaterPhotosUrls.Any())
                partner.TheaterPhotosUrl = string.Join(";", request.TheaterPhotosUrls);

            partner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var theaterPhotos = string.IsNullOrEmpty(partner.TheaterPhotosUrl)
                ? new List<string>()
                : partner.TheaterPhotosUrl.Split(';').ToList();

            return new PartnerProfileResponse
            {
                UserId = partner.User.UserId,
                Email = partner.User.Email,
                Phone = partner.User.Phone,
                FullName = partner.User.Fullname,
                AvatarUrl = partner.User.AvatarUrl,

                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Address = partner.Address,
                CommissionRate = partner.CommissionRate,

                BusinessRegistrationCertificateUrl = partner.BusinessRegistrationCertificateUrl ?? "",
                TaxRegistrationCertificateUrl = partner.TaxRegistrationCertificateUrl ?? "",
                IdentityCardUrl = partner.IdentityCardUrl ?? "",
                TheaterPhotosUrls = theaterPhotos,

                Status = partner.Status,
                CreatedAt = partner.CreatedAt,
                UpdatedAt = partner.UpdatedAt
            };
        }

        private void ValidateUpdateRequiredFields(PartnerUpdateRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new ValidationError { Msg = "Họ và tên là bắt buộc", Path = "fullName" };

            if (string.IsNullOrWhiteSpace(request.Phone))
                errors["phone"] = new ValidationError { Msg = "Số điện thoại là bắt buộc", Path = "phone" };

            if (string.IsNullOrWhiteSpace(request.PartnerName))
                errors["partnerName"] = new ValidationError { Msg = "Tên doanh nghiệp là bắt buộc", Path = "partnerName" };

            if (string.IsNullOrWhiteSpace(request.TaxCode))
                errors["taxCode"] = new ValidationError { Msg = "Mã số thuế là bắt buộc", Path = "taxCode" };

            if (string.IsNullOrWhiteSpace(request.Address))
                errors["address"] = new ValidationError { Msg = "Địa chỉ kinh doanh là bắt buộc", Path = "address" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateBusinessInfoForUpdateAsync(int userId, string partnerName, string taxCode)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (partnerName.Length < 2)
                errors.TryAdd("partnerName", new ValidationError { Msg = "Tên doanh nghiệp phải có ít nhất 2 ký tự", Path = "partnerName" });
            else if (partnerName.Length > 255)
                errors.TryAdd("partnerName", new ValidationError { Msg = "Tên doanh nghiệp không được vượt quá 255 ký tự", Path = "partnerName" });

            if (taxCode.Length != 10 && taxCode.Length != 13)
                errors.TryAdd("taxCode", new ValidationError { Msg = "Mã số thuế phải có 10 hoặc 13 chữ số", Path = "taxCode" });
            else if (!Regex.IsMatch(taxCode, @"^\d+$"))
                errors.TryAdd("taxCode", new ValidationError { Msg = "Mã số thuế chỉ được chứa chữ số", Path = "taxCode" });

            if (errors.Any())
                throw new ValidationException(errors);

            if (await _context.Partners.AnyAsync(p => p.PartnerName == partnerName && p.UserId != userId))
                throw new ConflictException("partnerName", "Tên doanh nghiệp đã tồn tại");

            if (await _context.Partners.AnyAsync(p => p.TaxCode == taxCode && p.UserId != userId))
                throw new ConflictException("taxCode", "Mã số thuế đã được đăng ký");
        }

        private void ValidateDocuments(PartnerUpdateRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            var urlPattern = @"^https?://.+\.(jpg|jpeg|png|pdf|doc|docx)$";

            if (!string.IsNullOrWhiteSpace(request.BusinessRegistrationCertificateUrl) &&
                !Regex.IsMatch(request.BusinessRegistrationCertificateUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("businessRegistrationCertificateUrl", new ValidationError { Msg = "Giấy ĐKDN phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "businessRegistrationCertificateUrl" });

            if (!string.IsNullOrWhiteSpace(request.TaxRegistrationCertificateUrl) &&
                !Regex.IsMatch(request.TaxRegistrationCertificateUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("taxRegistrationCertificateUrl", new ValidationError { Msg = "Giấy ĐKT phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "taxRegistrationCertificateUrl" });

            if (!string.IsNullOrWhiteSpace(request.IdentityCardUrl) &&
                !Regex.IsMatch(request.IdentityCardUrl, urlPattern, RegexOptions.IgnoreCase))
                errors.TryAdd("identityCardUrl", new ValidationError { Msg = "Ảnh CMND/CCCD phải là URL hợp lệ (jpg, png, pdf, doc)", Path = "identityCardUrl" });

            if (errors.Any())
                throw new ValidationException(errors);
        }
        public async Task<PaginatedPartnersResponse> GetPendingPartnersAsync(
    int page = 1,
    int limit = 10,
    string? search = null,
    string? sortBy = "created_at",
    string? sortOrder = "desc")
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            // Base query - chỉ lấy partners có status = "pending"
            var query = _context.Partners
                .Include(p => p.User)
                .Where(p => p.Status == "pending")
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.PartnerName.ToLower().Contains(search) ||
                    p.Email.ToLower().Contains(search) ||
                    p.Phone.ToLower().Contains(search) ||
                    p.TaxCode.ToLower().Contains(search) ||
                    (p.User != null && p.User.Fullname.ToLower().Contains(search)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplyPendingPartnersSorting(query, sortBy, sortOrder);

            // Apply pagination - SỬA LẠI PHÉP TÍNH
            var partners = await query
                .Skip((page - 1) * limit)  // Sửa thành: (page - 1) * limit
                .Take(limit)
                .Select(p => new PartnerPendingResponse
                {
                    PartnerId = p.PartnerId,
                    PartnerName = p.PartnerName,
                    TaxCode = p.TaxCode,  // Sửa từ LatCode -> TaxCode
                    Address = p.Address,
                    Email = p.Email,
                    Phone = p.Phone,
                    CommissionRate = p.CommissionRate,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt, 
                    UserId = p.UserId,
                    Fullname = p.User != null ? p.User.Fullname : "",
                    UserEmail = p.User != null ? p.User.Email : "",
                    UserPhone = p.User != null ? p.User.Phone : "",

                    BusinessRegistrationCertificateUrl = p.BusinessRegistrationCertificateUrl,
                    TaxRegistrationCertificateUrl = p.TaxRegistrationCertificateUrl,
                    IdentityCardUrl = p.IdentityCardUrl,
                    TheaterPhotosUrl = p.TheaterPhotosUrl
                })
                .ToListAsync();  

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedPartnersResponse
            {
                Partners = partners,
                Pagination = pagination
            };
        }
        public async Task<PartnerApprovalResponse> ApprovePartnerAsync(int partnerId, int managerId) // XÓA request parameter
        {
            var partner = await _context.Partners
                .Include(p => p.User)
                .Include(p => p.Manager)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner với ID này.");

            var manager = await _context.Managers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ManagerId == managerId);

            if (manager == null)
                throw new UnauthorizedException("Manager không tồn tại.");

            // Business logic validation
            ValidatePartnerForApproval(partner);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật thông tin partner - CHỈ SET STATUS VÀ ACTIVE
            partner.Status = "approved";
            partner.ApprovedAt = DateTime.UtcNow;
            partner.ApprovedBy = managerId;
            partner.UpdatedAt = DateTime.UtcNow;
            partner.ManagerId = managerId;

            // Kích hoạt tài khoản user - SET EMAIL_CONFIRMED VÀ IS_ACTIVE
            if (partner.User != null)
            {
                partner.User.IsActive = true;
                partner.User.EmailConfirmed = true; // QUAN TRỌNG: Xác thực email
                partner.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo cho partner
            await SendPartnerApprovalEmailAsync(partner, manager);

            return new PartnerApprovalResponse
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Status = partner.Status,
                CommissionRate = partner.CommissionRate, // Giữ nguyên rate từ đăng ký
                ApprovedAt = partner.ApprovedAt.Value,
                ApprovedBy = managerId,
                ManagerName = manager.User?.Fullname ?? "",

                // User information
                UserId = partner.UserId,
                Fullname = partner.User?.Fullname ?? "",
                Email = partner.User?.Email ?? "",
                Phone = partner.User?.Phone ?? "",
                IsActive = partner.User?.IsActive ?? false,
                EmailConfirmed = partner.User?.EmailConfirmed ?? false
            };
        }

        private void ValidatePartnerForApproval(Partner partner)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (partner.Status != "pending")
                errors["status"] = new ValidationError
                {
                    Msg = $"Chỉ có thể duyệt partner với trạng thái 'pending'. Hiện tại: {partner.Status}",
                    Path = "status"
                };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task SendPartnerApprovalEmailAsync(Partner partner, Manager manager)
        {
            try
            {
                if (partner.User?.Email != null)
                {
                    var subject = "ĐƠN ĐĂNG KÝ ĐỐI TÁC ĐÃ ĐƯỢC DUYỆT";
                    var body = $"""
            Kính gửi Ông/Bà {partner.User.Fullname},

            Chúc mừng! Đơn đăng ký đối tác của Quý công ty {partner.PartnerName} đã được duyệt.

            Thông tin duyệt:
            - Mã đối tác: {partner.PartnerId}
            - Tỷ lệ hoa hồng: {partner.CommissionRate}%
            - Người duyệt: {manager.User?.Fullname}
            - Thời gian duyệt: {DateTime.UtcNow:dd/MM/yyyy HH:mm}

            Từ thời điểm này, Quý đối tác có thể đăng nhập vào hệ thống Express Ticket Cinema 
            để quản lý rạp chiếu phim và bắt đầu hợp tác kinh doanh.

            Trân trọng,
            TICKET EXPRESS
            """;

                    await _emailService.SendEmailAsync(partner.User.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng business logic
                Console.WriteLine($"Failed to send approval email: {ex.Message}");
            }
        }
        public async Task<PartnerRejectionResponse> RejectPartnerAsync(int partnerId, int managerId, RejectPartnerRequest request)
        {
            ValidateRejectRequest(request);

            var partner = await _context.Partners
                .Include(p => p.User)
                .Include(p => p.Manager)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner với ID này.");

            var manager = await _context.Managers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ManagerId == managerId);

            if (manager == null)
                throw new UnauthorizedException("Manager không tồn tại.");

            ValidatePartnerForRejection(partner);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật thông tin partner
            partner.Status = "rejected";
            partner.RejectionReason = request.RejectionReason;
            partner.UpdatedAt = DateTime.UtcNow;
            partner.ManagerId = managerId;

            // DEACTIVATE tài khoản user - SET VỀ false
            if (partner.User != null)
            {
                partner.User.IsActive = false;
                partner.User.EmailConfirmed = false; // QUAN TRỌNG: Hủy xác thực email
                partner.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo cho partner
            await SendPartnerRejectionEmailAsync(partner, manager, request.RejectionReason);

            return new PartnerRejectionResponse
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Status = partner.Status,
                RejectionReason = partner.RejectionReason,
                RejectedAt = DateTime.UtcNow,
                RejectedBy = managerId,
                ManagerName = manager.User?.Fullname ?? "",

                // User information
                UserId = partner.UserId,
                Fullname = partner.User?.Fullname ?? "",
                Email = partner.User?.Email ?? "",
                Phone = partner.User?.Phone ?? "",
                IsActive = partner.User?.IsActive ?? false,
                EmailConfirmed = partner.User?.EmailConfirmed ?? false
            };
        }

        private void ValidateRejectRequest(RejectPartnerRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                errors["rejectionReason"] = new ValidationError { Msg = "Lý do từ chối là bắt buộc", Path = "rejectionReason" };
            else if (request.RejectionReason.Trim().Length < 10)
                errors["rejectionReason"] = new ValidationError { Msg = "Lý do từ chối phải có ít nhất 10 ký tự", Path = "rejectionReason" };
            else if (request.RejectionReason.Length > 1000)
                errors["rejectionReason"] = new ValidationError { Msg = "Lý do từ chối không được vượt quá 1000 ký tự", Path = "rejectionReason" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidatePartnerForRejection(Partner partner)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (partner.Status != "pending")
                errors["status"] = new ValidationError { Msg = $"Chỉ có thể từ chối partner với trạng thái 'pending'. Hiện tại: {partner.Status}", Path = "status" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task SendPartnerRejectionEmailAsync(Partner partner, Manager manager, string rejectionReason)
        {
            try
            {
                if (partner.User?.Email != null)
                {
                    var subject = "THÔNG BÁO TỪ CHỐI ĐƠN ĐĂNG KÝ ĐỐI TÁC";
                    var body = $"""
            Kính gửi Ông/Bà {partner.User.Fullname},

            Chúng tôi rất tiếc phải thông báo rằng đơn đăng ký đối tác của Quý công ty {partner.PartnerName} 
            không được chấp thuận tại thời điểm này.

            Lý do từ chối:
            {rejectionReason}

            Thông tin:
            - Mã đối tác: {partner.PartnerId}
            - Người xử lý: {manager.User?.Fullname}
            - Thời gian: {DateTime.UtcNow:dd/MM/yyyy HH:mm}

            Nếu Quý đối tác có bất kỳ thắc mắc nào hoặc muốn cung cấp thêm thông tin, 
            vui lòng liên hệ với chúng tôi để được hỗ trợ.

            Trân trọng,
            TICKET EXPRESS
            """;

                    await _emailService.SendEmailAsync(partner.User.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng business logic
                Console.WriteLine($"Failed to send rejection email: {ex.Message}");
            }
        }
        private IQueryable<Partner> ApplyPendingPartnersSorting(IQueryable<Partner> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "created_at";
            sortOrder = sortOrder?.ToLower() ?? "desc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "partner_name" => isAscending ? query.OrderBy(p => p.PartnerName) : query.OrderByDescending(p => p.PartnerName),
                "email" => isAscending ? query.OrderBy(p => p.Email) : query.OrderByDescending(p => p.Email),
                "phone" => isAscending ? query.OrderBy(p => p.Phone) : query.OrderByDescending(p => p.Phone),
                "tax_code" => isAscending ? query.OrderBy(p => p.TaxCode) : query.OrderByDescending(p => p.TaxCode),
                "updated_at" => isAscending ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
                _ => isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt) // default
            };
        }
     }
 }
