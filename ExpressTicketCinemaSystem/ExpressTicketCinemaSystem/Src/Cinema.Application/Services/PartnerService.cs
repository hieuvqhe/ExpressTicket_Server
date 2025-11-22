using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using PartnerResponses = ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using System.Text;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class PartnerService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IManagerService _managerService;
        private readonly IContractValidationService _contractValidationService;

        public PartnerService(
            CinemaDbCoreContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService,
            IManagerService managerService ,
            IContractValidationService contractValidationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _managerService = managerService;
            _contractValidationService = contractValidationService;
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
            ValidateAdditionalDocuments(request.AdditionalDocumentsUrls);

            // ==================== BUSINESS LOGIC SECTION ====================
            var defaultManager = await _context.Managers
       .OrderBy(m => m.ManagerId)
       .FirstOrDefaultAsync();

            if (defaultManager == null)
            {
                throw new InvalidOperationException("Không tìm thấy manager để phân công");
            }
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
                ManagerId = defaultManager.ManagerId, 
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
                AdditionalDocumentsUrl = request.AdditionalDocumentsUrls != null && request.AdditionalDocumentsUrls.Any() 
                    ? string.Join(";", request.AdditionalDocumentsUrls) 
                    : null,
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

        private void ValidateAdditionalDocuments(List<string> additionalDocumentsUrls)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Optional field - chỉ validate nếu có giá trị
            if (additionalDocumentsUrls != null && additionalDocumentsUrls.Any())
            {
                if (additionalDocumentsUrls.Count > 10)
                    errors.TryAdd("additionalDocumentsUrls", new ValidationError { Msg = "Chỉ được phép tối đa 10 ảnh giấy tờ khác", Path = "additionalDocumentsUrls" });
                else
                {
                    var urlPattern = @"^https?://.+\.(jpg|jpeg|png|webp|svg)$";
                    foreach (var documentUrl in additionalDocumentsUrls)
                    {
                        if (string.IsNullOrWhiteSpace(documentUrl))
                        {
                            errors.TryAdd("additionalDocumentsUrls_empty", new ValidationError { Msg = "URL ảnh giấy tờ không được để trống", Path = "additionalDocumentsUrls" });
                            break;
                        }
                        if (!Regex.IsMatch(documentUrl, urlPattern, RegexOptions.IgnoreCase))
                        {
                            errors.TryAdd("additionalDocumentsUrls_pattern", new ValidationError { Msg = $"URL ảnh giấy tờ không hợp lệ: {documentUrl}. Phải là URL ảnh (jpg, jpeg, png, webp, svg)", Path = "additionalDocumentsUrls" });
                            break;
                        }
                    }

                    if (additionalDocumentsUrls.Distinct().Count() != additionalDocumentsUrls.Count)
                        errors.TryAdd("additionalDocumentsUrls_duplicate", new ValidationError { Msg = "Không được phép có URL ảnh trùng lặp", Path = "additionalDocumentsUrls" });
                }
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

            var additionalDocuments = string.IsNullOrEmpty(partner.AdditionalDocumentsUrl)
                ? new List<string>()
                : partner.AdditionalDocumentsUrl.Split(';').ToList();

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
                AdditionalDocumentsUrls = additionalDocuments,

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
            ValidateAdditionalDocuments(request.AdditionalDocumentsUrls);

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

            if (request.AdditionalDocumentsUrls != null && request.AdditionalDocumentsUrls.Any())
                partner.AdditionalDocumentsUrl = string.Join(";", request.AdditionalDocumentsUrls);
            else if (request.AdditionalDocumentsUrls != null && !request.AdditionalDocumentsUrls.Any())
                partner.AdditionalDocumentsUrl = null;

            partner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var theaterPhotos = string.IsNullOrEmpty(partner.TheaterPhotosUrl)
                ? new List<string>()
                : partner.TheaterPhotosUrl.Split(';').ToList();

            var additionalDocuments = string.IsNullOrEmpty(partner.AdditionalDocumentsUrl)
                ? new List<string>()
                : partner.AdditionalDocumentsUrl.Split(';').ToList();

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
                AdditionalDocumentsUrls = additionalDocuments,

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
                    TheaterPhotosUrl = p.TheaterPhotosUrl,
                    AdditionalDocumentsUrl = p.AdditionalDocumentsUrl
                })
                .ToListAsync();  

            var pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
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
        public async Task<PartnerApprovalResponse> ApprovePartnerAsync(int partnerId, int managerId)
        {
            var partner = await _context.Partners
            .Include(p => p.User)
            .Include(p => p.Manager)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner với ID này.");

            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                var defaultManagerId = await _managerService.GetDefaultManagerIdAsync();
                managerId = defaultManagerId;
            }

            var manager = await _context.Managers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ManagerId == managerId);

            if (manager == null)
                throw new UnauthorizedException("Manager không tồn tại.");

            // Business logic validation
            ValidatePartnerForApproval(partner);

            // ==================== BUSINESS LOGIC SECTION ====================

            partner.Status = "approved";
            partner.ApprovedAt = DateTime.UtcNow;
            partner.ApprovedBy = manager.ManagerId; // ← DÙNG manager.ManagerId THAY VÌ managerId parameter
            partner.UpdatedAt = DateTime.UtcNow;
            partner.ManagerId = manager.ManagerId;  // ← DÙNG manager.ManagerId THAY VÌ managerId parameter

            if (partner.User != null)
            {
                partner.User.IsActive = true;
                partner.User.EmailConfirmed = true;
                partner.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await SendPartnerApprovalEmailAsync(partner, manager);

            return new PartnerApprovalResponse
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Status = partner.Status,
                CommissionRate = partner.CommissionRate,
                ApprovedAt = partner.ApprovedAt.Value,
                ApprovedBy = manager.ManagerId, 
                ManagerName = manager.User?.Fullname ?? "",

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
                    var subject = "THÔNG BÁO DUYỆT ĐƠN ĐĂNG KÝ ĐỐI TÁC THÀNH CÔNG";

                    var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 30px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9;'>
        <div style='text-align: center; margin-bottom: 20px;'>
            <div style='font-size: 48px; margin-bottom: 10px;'>🎉</div>
            <h2 style='color: #28a745; margin-bottom: 10px;'>CHÚC MỪNG!</h2>
            <p style='color: #666; font-size: 18px;'>Đơn đăng ký đối tác đã được duyệt thành công</p>
        </div>
        
        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #28a745;'>
            <p style='margin-bottom: 10px;'>Kính gửi Ông/Bà <strong>{partner.User.Fullname}</strong>,</p>
            <p style='margin-bottom: 20px;'>Đơn đăng ký đối tác của Quý công ty <strong>{partner.PartnerName}</strong> đã được duyệt thành công.</p>
            
            <h4 style='color: #333; margin-bottom: 15px;'> THÔNG TIN DUYỆT:</h4>
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px;'>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666; width: 140px;'>Mã đối tác:</td>
                        <td style='padding: 8px 0;'><strong>{partner.PartnerId}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tên công ty:</td>
                        <td style='padding: 8px 0;'><strong>{partner.PartnerName}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tỷ lệ hoa hồng:</td>
                        <td style='padding: 8px 0;'><strong style='color: #28a745;'>{partner.CommissionRate}%</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Người duyệt:</td>
                        <td style='padding: 8px 0;'><strong>{manager.User?.Fullname ?? "Hệ thống"}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Thời gian duyệt:</td>
                        <td style='padding: 8px 0;'><strong>{DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm} (GMT+7)</strong></td>
                    </tr>
                </table>
            </div>
            
            <div style='margin-top: 25px; padding: 20px; background: #d4edda; border-radius: 5px; border: 1px solid #c3e6cb;'>
                <h4 style='color: #155724; margin: 0 0 10px 0;'> Bắt đầu ngay:</h4>
                <p style='margin: 0; color: #155724;'>
                    Từ thời điểm này, Quý đối tác có thể đăng nhập vào hệ thống 
                    <strong>Express Ticket Cinema</strong> để quản lý rạp chiếu phim và bắt đầu 
                    thực hiện ký hợp đồng hợp tác kinh doanh.
                </p>
            </div>
        </div>
    </div>
    
    <div style='padding: 20px; text-align: center; background: #333; color: white;'>
        <p style='margin: 0 0 10px 0; font-size: 16px; font-weight: bold;'>ĐỘI NGŨ HỖ TRỢ TICKET EXPRESS</p>
        <p style='margin: 5px 0;'>Hotline: 1900 1234 | Email: support@ticketexpress.com</p>
        <p style='margin: 15px 0 0 0; font-size: 12px; opacity: 0.8;'>
            © 2024 TicketExpress. All rights reserved.<br>
            Đây là email tự động, vui lòng không trả lời.
        </p>
    </div>
</div>";

                    await _emailService.SendEmailAsync(partner.User.Email, subject, htmlBody);
                }
            }
            catch (Exception ex)
            {
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

            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                var defaultManagerId = await _managerService.GetDefaultManagerIdAsync();
                managerId = defaultManagerId;
            }

            var manager = await _context.Managers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ManagerId == managerId);

            if (manager == null)
                throw new UnauthorizedException("Manager không tồn tại.");

            ValidatePartnerForRejection(partner);

            // ==================== BUSINESS LOGIC SECTION ====================

            partner.Status = "rejected";
            partner.RejectionReason = request.RejectionReason;
            partner.UpdatedAt = DateTime.UtcNow;
            partner.ManagerId = manager.ManagerId;

            if (partner.User != null)
            {
                partner.User.IsActive = false;
                partner.User.EmailConfirmed = false;
                partner.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await SendPartnerRejectionEmailAsync(partner, manager, request.RejectionReason);

            return new PartnerRejectionResponse
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                TaxCode = partner.TaxCode,
                Status = partner.Status,
                RejectionReason = partner.RejectionReason,
                RejectedAt = DateTime.UtcNow,
                RejectedBy = manager.ManagerId, // ← DÙNG manager.ManagerId
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

                    var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); padding: 30px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9;'>
        <h2 style='color: #333; margin-bottom: 20px;'>Thông báo từ chối đơn đăng ký đối tác</h2>
        
        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #dc3545;'>
            <p style='margin-bottom: 10px;'>Kính gửi Ông/Bà <strong>{partner.User.Fullname}</strong>,</p>
            <p style='margin-bottom: 15px;'>Chúng tôi rất tiếc phải thông báo rằng đơn đăng ký đối tác của Quý công ty <strong>{partner.PartnerName}</strong> không được chấp thuận tại thời điểm này.</p>
            
            <div style='background: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <h4 style='color: #721c24; margin: 0 0 10px 0;'>📝 LÝ DO TỪ CHỐI:</h4>
                <p style='margin: 0; color: #721c24; line-height: 1.5;'>{rejectionReason}</p>
            </div>
            
            <h4 style='color: #333; margin-bottom: 15px;'>📋 THÔNG TIN CHI TIẾT:</h4>
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px;'>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666; width: 140px;'>Mã đối tác:</td>
                        <td style='padding: 8px 0;'><strong>{partner.PartnerId}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tên công ty:</td>
                        <td style='padding: 8px 0;'><strong>{partner.PartnerName}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Người xử lý:</td>
                        <td style='padding: 8px 0;'><strong>{manager.User?.Fullname ?? "Hệ thống"}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Thời gian:</td>
                        <td style='padding: 8px 0;'><strong>{DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm} (GMT+7)</strong></td>
                    </tr>
                </table>
            </div>
            
            <div style='margin-top: 25px; padding: 15px; background: #e7f3ff; border-radius: 5px;'>
                <p style='margin: 0; color: #0c5460;'>
                    <strong>💡 Thông tin hỗ trợ:</strong><br>
                    Nếu Quý đối tác có bất kỳ thắc mắc nào hoặc muốn cung cấp thêm thông tin, 
                    vui lòng liên hệ với chúng tôi để được hỗ trợ.
                </p>
            </div>
        </div>
    </div>
    
    <div style='padding: 20px; text-align: center; background: #333; color: white;'>
        <p style='margin: 0 0 10px 0; font-size: 16px; font-weight: bold;'>ĐỘI NGŨ HỖ TRỢ TICKET EXPRESS</p>
        <p style='margin: 5px 0;'>Hotline: 1900 1234 | Email: support@ticketexpress.com</p>
        <p style='margin: 15px 0 0 0; font-size: 12px; opacity: 0.8;'>
            © 2024 TicketExpress. All rights reserved.<br>
            Đây là email tự động, vui lòng không trả lời.
        </p>
    </div>
</div>";

                    await _emailService.SendEmailAsync(partner.User.Email, subject, htmlBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send rejection email: {ex.Message}");
            }
        }
        public async Task<PaginatedPartnersWithoutContractsResponse> GetPartnersWithoutContractsAsync(
    int page = 1,
    int limit = 10,
    string? search = null)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var partnersWithContracts = _context.Contracts
                .Where(c => c.Status != "draft") 
                .Select(c => c.PartnerId)
                .Distinct();

            var query = _context.Partners
                .Where(p => p.Status == "approved" && !partnersWithContracts.Contains(p.PartnerId))
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.PartnerName.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            var partners = await query
                .OrderBy(p => p.PartnerName)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new PartnerWithoutContractResponse
                {
                    PartnerId = p.PartnerId,
                    PartnerName = p.PartnerName
                })
                .ToListAsync();

            var pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedPartnersWithoutContractsResponse
            {
                Partners = partners,
                Pagination = pagination
            };
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

        /// <summary>
        /// Get partner's bookings from their cinemas with filtering and pagination
        /// </summary>
        public async Task<PartnerBookingsResponse> GetPartnerBookingsAsync(int userId, GetPartnerBookingsRequest request)
        {
            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Validate sort parameters
            var validSortBy = new[] { "booking_time", "total_amount", "created_at" };
            if (!validSortBy.Contains(request.SortBy.ToLower()))
                throw new ValidationException("sortBy", "SortBy phải là một trong: booking_time, total_amount, created_at.");

            var validSortOrder = new[] { "asc", "desc" };
            if (!validSortOrder.Contains(request.SortOrder.ToLower()))
                throw new ValidationException("sortOrder", "SortOrder phải là asc hoặc desc.");

            // Get partner from userId
            var partner = await _context.Partners
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy thông tin đối tác hoặc tài khoản chưa được kích hoạt.");

            // Get list of cinema IDs belonging to this partner
            var partnerCinemaIds = await _context.Cinemas
                .Where(c => c.PartnerId == partner.PartnerId && c.IsActive == true)
                .Select(c => c.CinemaId)
                .ToListAsync();

            if (!partnerCinemaIds.Any())
            {
                // Partner has no cinemas
                return new PartnerBookingsResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new List<PartnerBookingItemDto>()
                };
            }

            // If cinemaId is specified, verify it belongs to this partner
            if (request.CinemaId.HasValue)
            {
                if (!partnerCinemaIds.Contains(request.CinemaId.Value))
                    throw new ValidationException("cinemaId", "Rạp này không thuộc về đối tác của bạn.");
            }

            // Build query
            var query = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                .Where(b => partnerCinemaIds.Contains(b.Showtime.CinemaId))
                .AsNoTracking();

            // Apply filters
            if (request.CinemaId.HasValue)
            {
                query = query.Where(b => b.Showtime.CinemaId == request.CinemaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusLower = request.Status.Trim().ToLower();
                query = query.Where(b =>
                    b.Status.ToLower() == statusLower ||
                    b.State.ToLower() == statusLower);
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                var paymentStatusLower = request.PaymentStatus.Trim().ToLower();
                query = query.Where(b => b.PaymentStatus != null && b.PaymentStatus.ToLower() == paymentStatusLower);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(b => b.BookingTime >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                var toDateEndOfDay = request.ToDate.Value.Date.AddDays(1);
                query = query.Where(b => b.BookingTime < toDateEndOfDay);
            }

            if (request.CustomerId.HasValue)
            {
                query = query.Where(b => b.CustomerId == request.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                var emailSearch = request.CustomerEmail.Trim().ToLower();
                query = query.Where(b => b.Customer.User.Email != null && b.Customer.User.Email.ToLower().Contains(emailSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                var phoneSearch = request.CustomerPhone.Trim();
                query = query.Where(b => b.Customer.User.Phone != null && b.Customer.User.Phone.Contains(phoneSearch));
            }

            if (!string.IsNullOrWhiteSpace(request.BookingCode))
            {
                var bookingCodeSearch = request.BookingCode.Trim().ToUpper();
                query = query.Where(b => b.BookingCode.ToUpper().Contains(bookingCodeSearch));
            }

            // Get total count
            var totalItems = await query.CountAsync();

            // Apply sorting
            var sortBy = request.SortBy.ToLower();
            var isAscending = request.SortOrder.ToLower() == "asc";

            query = sortBy switch
            {
                "total_amount" => isAscending ? query.OrderBy(b => b.TotalAmount) : query.OrderByDescending(b => b.TotalAmount),
                "created_at" => isAscending ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                _ => isAscending ? query.OrderBy(b => b.BookingTime) : query.OrderByDescending(b => b.BookingTime) // default booking_time
            };

            // Apply pagination
            var bookings = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to response DTO
            var items = bookings.Select(b => new PartnerBookingItemDto
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                BookingTime = b.BookingTime,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                State = b.State,
                PaymentStatus = b.PaymentStatus,
                PaymentProvider = b.PaymentProvider,
                PaymentTxId = b.PaymentTxId,
                OrderCode = b.OrderCode,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                TicketCount = b.Tickets.Count,
                Customer = new PartnerBookingCustomerDto
                {
                    CustomerId = b.Customer.CustomerId,
                    UserId = b.Customer.UserId,
                    Fullname = b.Customer.User.Fullname,
                    Email = b.Customer.User.Email,
                    Phone = b.Customer.User.Phone
                },
                Showtime = new PartnerBookingShowtimeDto
                {
                    ShowtimeId = b.Showtime.ShowtimeId,
                    ShowDatetime = b.Showtime.ShowDatetime,
                    EndTime = b.Showtime.EndTime,
                    FormatType = b.Showtime.FormatType,
                    Status = b.Showtime.Status
                },
                Cinema = new PartnerBookingCinemaDto
                {
                    CinemaId = b.Showtime.Cinema.CinemaId,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    Address = b.Showtime.Cinema.Address,
                    City = b.Showtime.Cinema.City,
                    District = b.Showtime.Cinema.District
                },
                Movie = new PartnerBookingMovieDto
                {
                    MovieId = b.Showtime.Movie.MovieId,
                    Title = b.Showtime.Movie.Title,
                    DurationMinutes = b.Showtime.Movie.DurationMinutes,
                    PosterUrl = b.Showtime.Movie.PosterUrl,
                    Genre = b.Showtime.Movie.Genre
                }
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

            return new PartnerBookingsResponse
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }

        /// <summary>
        /// Get partner's booking detail by booking ID
        /// Partner can only view bookings from their own cinemas
        /// </summary>
        public async Task<PartnerBookingDetailResponse> GetPartnerBookingDetailAsync(int userId, int bookingId)
        {
            // Get partner from userId
            var partner = await _context.Partners
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy thông tin đối tác hoặc tài khoản chưa được kích hoạt.");

            // Get list of cinema IDs belonging to this partner
            var partnerCinemaIds = await _context.Cinemas
                .Where(c => c.PartnerId == partner.PartnerId && c.IsActive == true)
                .Select(c => c.CinemaId)
                .ToListAsync();

            if (!partnerCinemaIds.Any())
                throw new NotFoundException("Đối tác chưa có rạp chiếu phim nào.");

            // Get booking with all related data
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Screen)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                .Include(b => b.ServiceOrders)
                    .ThenInclude(so => so.Service)
                .Include(b => b.Voucher)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                throw new NotFoundException("Không tìm thấy đơn đặt vé với ID đã cho.");

            // Verify that the booking belongs to one of partner's cinemas
            if (!partnerCinemaIds.Contains(booking.Showtime.CinemaId))
                throw new NotFoundException("Không tìm thấy đơn đặt vé với ID đã cho.");

            // Map to response DTO
            var response = new PartnerBookingDetailResponse
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                BookingTime = booking.BookingTime,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                State = booking.State,
                PaymentStatus = booking.PaymentStatus,
                PaymentProvider = booking.PaymentProvider,
                PaymentTxId = booking.PaymentTxId,
                OrderCode = booking.OrderCode,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                Customer = new PartnerBookingCustomerDto
                {
                    CustomerId = booking.Customer.CustomerId,
                    UserId = booking.Customer.UserId,
                    Fullname = booking.Customer.User.Fullname,
                    Email = booking.Customer.User.Email,
                    Phone = booking.Customer.User.Phone
                },
                Showtime = new PartnerBookingShowtimeDto
                {
                    ShowtimeId = booking.Showtime.ShowtimeId,
                    ShowDatetime = booking.Showtime.ShowDatetime,
                    EndTime = booking.Showtime.EndTime,
                    FormatType = booking.Showtime.FormatType,
                    Status = booking.Showtime.Status
                },
                Cinema = new PartnerBookingCinemaDto
                {
                    CinemaId = booking.Showtime.Cinema.CinemaId,
                    CinemaName = booking.Showtime.Cinema.CinemaName,
                    Address = booking.Showtime.Cinema.Address,
                    City = booking.Showtime.Cinema.City,
                    District = booking.Showtime.Cinema.District
                },
                Movie = new PartnerBookingMovieDto
                {
                    MovieId = booking.Showtime.Movie.MovieId,
                    Title = booking.Showtime.Movie.Title,
                    DurationMinutes = booking.Showtime.Movie.DurationMinutes,
                    PosterUrl = booking.Showtime.Movie.PosterUrl,
                    Genre = booking.Showtime.Movie.Genre
                },
                Tickets = booking.Tickets.Select(t => new PartnerBookingTicketDto
                {
                    TicketId = t.TicketId,
                    SeatId = t.SeatId,
                    SeatName = t.Seat.SeatName ?? $"{t.Seat.RowCode}{t.Seat.SeatNumber}",
                    RowCode = t.Seat.RowCode,
                    SeatNumber = t.Seat.SeatNumber,
                    Price = t.Price,
                    Status = t.Status
                }).ToList(),
                ServiceOrders = booking.ServiceOrders.Select(so => new PartnerBookingServiceOrderDto
                {
                    OrderId = so.OrderId,
                    ServiceId = so.ServiceId,
                    ServiceName = so.Service.ServiceName,
                    ServiceCode = so.Service.Code,
                    Quantity = so.Quantity,
                    UnitPrice = so.UnitPrice,
                    SubTotal = so.Quantity * so.UnitPrice
                }).ToList()
            };

            // Add voucher information if exists
            if (booking.Voucher != null)
            {
                response.Voucher = new PartnerBookingVoucherDto
                {
                    VoucherId = booking.Voucher.VoucherId,
                    VoucherCode = booking.Voucher.VoucherCode,
                    DiscountType = booking.Voucher.DiscountType,
                    DiscountValue = booking.Voucher.DiscountVal
                };
            }

            return response;
        }

        /// <summary>
        /// Get partner's booking statistics from their cinemas
        /// Partner can only view statistics from their own cinemas
        /// </summary>
        public async Task<PartnerBookingStatisticsResponse> GetPartnerBookingStatisticsAsync(int userId, GetPartnerBookingStatisticsRequest request)
        {
            // Validate top limit
            if (request.TopLimit < 1 || request.TopLimit > 50)
                throw new ValidationException("topLimit", "TopLimit phải trong khoảng 1-50.");

            // Validate pagination
            if (request.Page < 1)
                throw new ValidationException("page", "Page phải lớn hơn hoặc bằng 1.");

            if (request.PageSize < 1 || request.PageSize > 100)
                throw new ValidationException("pageSize", "PageSize phải trong khoảng 1-100.");

            // Validate groupBy
            var validGroupBy = new[] { "day", "week", "month", "year" };
            if (!validGroupBy.Contains(request.GroupBy.ToLower()))
                throw new ValidationException("groupBy", "GroupBy phải là một trong: day, week, month, year.");

            // Get partner from userId
            var partner = await _context.Partners
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy thông tin đối tác hoặc tài khoản chưa được kích hoạt.");

            // Get list of cinema IDs belonging to this partner
            var partnerCinemaIds = await _context.Cinemas
                .Where(c => c.PartnerId == partner.PartnerId && c.IsActive == true)
                .Select(c => c.CinemaId)
                .ToListAsync();

            if (!partnerCinemaIds.Any())
            {
                // Partner has no cinemas - return empty statistics
                return new PartnerBookingStatisticsResponse();
            }

            // If cinemaId is specified, verify it belongs to this partner
            if (request.CinemaId.HasValue)
            {
                if (!partnerCinemaIds.Contains(request.CinemaId.Value))
                    throw new ValidationException("cinemaId", "Rạp này không thuộc về đối tác của bạn.");
            }

            // Set default date range if not provided
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30).Date;
            var toDate = request.ToDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // End of day

            // Validate date range
            if (toDate < fromDate)
                throw new ValidationException("toDate", "ToDate phải lớn hơn hoặc bằng FromDate.");

            // Base query for all bookings in date range from partner's cinemas
            var baseQuery = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Screen)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(b => b.ServiceOrders)
                    .ThenInclude(so => so.Service)
                .Include(b => b.Voucher)
                .AsNoTracking()
                .Where(b => partnerCinemaIds.Contains(b.Showtime.CinemaId))
                .Where(b => b.BookingTime >= fromDate && b.BookingTime <= toDate);

            // Apply cinema filter if specified
            if (request.CinemaId.HasValue)
            {
                baseQuery = baseQuery.Where(b => b.Showtime.CinemaId == request.CinemaId.Value);
            }

            var bookings = await baseQuery.ToListAsync();

            var response = new PartnerBookingStatisticsResponse();

            // ========== OVERVIEW STATISTICS ==========
            response.Overview = CalculateOverviewStatistics(bookings);

            // ========== CINEMA REVENUE STATISTICS ==========
            response.CinemaRevenue = CalculateCinemaRevenueStatistics(bookings, request.TopLimit, request.Page, request.PageSize);

            // ========== MOVIE STATISTICS ==========
            response.MovieStatistics = CalculateMovieStatistics(bookings, request.TopLimit, request.Page, request.PageSize);

            // ========== TIME-BASED STATISTICS ==========
            response.TimeStatistics = await CalculateTimeBasedStatisticsAsync(bookings, fromDate, toDate, request.GroupBy, request.IncludeComparison, partnerCinemaIds);

            // ========== TOP CUSTOMERS STATISTICS ==========
            response.TopCustomers = CalculateTopCustomersStatistics(bookings, request.TopLimit, request.Page, request.PageSize);

            // ========== SERVICE STATISTICS ==========
            response.ServiceStatistics = CalculateServiceStatistics(bookings);

            // ========== SEAT STATISTICS ==========
            response.SeatStatistics = await CalculateSeatStatisticsAsync(bookings, partnerCinemaIds);

            // ========== SHOWTIME STATISTICS ==========
            response.ShowtimeStatistics = await CalculateShowtimeStatisticsAsync(bookings, partnerCinemaIds, request.TopLimit, request.Page, request.PageSize);

            // ========== PAYMENT STATISTICS ==========
            response.PaymentStatistics = CalculatePaymentStatistics(bookings);

            // ========== VOUCHER STATISTICS ==========
            response.VoucherStatistics = CalculateVoucherStatistics(bookings);

            return response;
        }

        /// <summary>
        /// Helper method to check if a booking is paid/completed
        /// </summary>
        private bool IsPaidBooking(Booking booking)
        {
            var paymentStatus = booking.PaymentStatus?.ToUpper() ?? "";
            var status = booking.Status.ToUpper();
            var state = booking.State.ToUpper();
            
            if (paymentStatus == "PAID")
                return true;
            
            if (status == "CONFIRMED" && paymentStatus == "PAID")
                return true;
            
            if (state == "COMPLETED" && paymentStatus == "PAID")
                return true;
            
            return false;
        }

        private PartnerResponses.BookingOverviewStatistics CalculateOverviewStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var totalTickets = paidBookings.Sum(b => b.Tickets.Count);
            var totalCustomers = bookings.Select(b => b.CustomerId).Distinct().Count();
            var totalRevenue = paidBookings.Sum(b => b.TotalAmount);
            var averageOrderValue = paidBookings.Any() ? totalRevenue / paidBookings.Count : 0;

            var bookingsByStatus = bookings
                .GroupBy(b => b.Status.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());

            var revenueByStatus = paidBookings
                .GroupBy(b => b.Status.ToUpper())
                .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalAmount));

            var bookingsByPaymentStatus = bookings
                .Where(b => !string.IsNullOrEmpty(b.PaymentStatus))
                .GroupBy(b => b.PaymentStatus!.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());

            return new PartnerResponses.BookingOverviewStatistics
            {
                TotalBookings = bookings.Count,
                TotalRevenue = totalRevenue,
                TotalPaidBookings = paidBookings.Count,
                TotalPendingBookings = bookings.Count(b => b.Status.ToUpper() == "PENDING_PAYMENT" || b.State.ToUpper() == "PENDING_PAYMENT"),
                TotalCancelledBookings = bookings.Count(b => b.Status.ToUpper() == "CANCELLED" || b.State.ToUpper() == "CANCELLED"),
                TotalTicketsSold = totalTickets,
                TotalCustomers = totalCustomers,
                AverageOrderValue = averageOrderValue,
                BookingsByStatus = bookingsByStatus,
                RevenueByStatus = revenueByStatus,
                BookingsByPaymentStatus = bookingsByPaymentStatus
            };
        }

        private PartnerResponses.CinemaRevenueStatistics CalculateCinemaRevenueStatistics(List<Booking> bookings, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var cinemaStats = paidBookings
                .GroupBy(b => new
                {
                    CinemaId = b.Showtime.Cinema.CinemaId,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    City = b.Showtime.Cinema.City,
                    District = b.Showtime.Cinema.District,
                    Address = b.Showtime.Cinema.Address
                })
                .Select(g => new PartnerResponses.CinemaRevenueStat
                {
                    CinemaId = g.Key.CinemaId,
                    CinemaName = g.Key.CinemaName,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    AverageOrderValue = g.Average(b => b.TotalAmount),
                    City = g.Key.City,
                    District = g.Key.District,
                    Address = g.Key.Address
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            var totalCount = cinemaStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var paginatedList = cinemaStats
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var topCinemas = cinemaStats.Take(topLimit).ToList();

            var comparison = new PartnerResponses.CinemaRevenueComparison
            {
                HighestRevenueCinema = cinemaStats.FirstOrDefault(),
                LowestRevenueCinema = cinemaStats.LastOrDefault(c => c.TotalRevenue > 0),
                AverageRevenuePerCinema = cinemaStats.Any() ? cinemaStats.Average(c => c.TotalRevenue) : 0
            };

            return new PartnerResponses.CinemaRevenueStatistics
            {
                CinemaRevenueList = paginatedList,
                TopCinemasByRevenue = topCinemas,
                Comparison = cinemaStats.Any() ? comparison : null,
                Pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                }
            };
        }

        private PartnerResponses.MovieRevenueStatistics CalculateMovieStatistics(List<Booking> bookings, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var movieStats = paidBookings
                .GroupBy(b => new
                {
                    MovieId = b.Showtime.Movie.MovieId,
                    Title = b.Showtime.Movie.Title,
                    Genre = b.Showtime.Movie.Genre
                })
                .Select(g => new PartnerResponses.MovieRevenueStat
                {
                    MovieId = g.Key.MovieId,
                    Title = g.Key.Title,
                    Genre = g.Key.Genre,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    ShowtimeCount = g.Select(b => b.ShowtimeId).Distinct().Count()
                })
                .ToList();

            var moviesByRevenue = movieStats
                .OrderByDescending(m => m.TotalRevenue)
                .ToList();

            var moviesByTickets = movieStats
                .OrderByDescending(m => m.TotalTicketsSold)
                .ToList();

            var topByRevenue = moviesByRevenue.Take(topLimit).ToList();
            var topByTickets = moviesByTickets.Take(topLimit).ToList();

            // Pagination for revenue list
            var totalCountByRevenue = moviesByRevenue.Count;
            var totalPagesByRevenue = (int)Math.Ceiling(totalCountByRevenue / (double)pageSize);
            var paginatedByRevenue = moviesByRevenue
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pagination for tickets list
            var totalCountByTickets = moviesByTickets.Count;
            var totalPagesByTickets = (int)Math.Ceiling(totalCountByTickets / (double)pageSize);
            var paginatedByTickets = moviesByTickets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PartnerResponses.MovieRevenueStatistics
            {
                TopMoviesByRevenue = topByRevenue,
                TopMoviesByTickets = topByTickets,
                PaginationByRevenue = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCountByRevenue,
                    TotalPages = totalPagesByRevenue
                },
                PaginationByTickets = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCountByTickets,
                    TotalPages = totalPagesByTickets
                }
            };
        }

        private async Task<PartnerResponses.TimeBasedStatistics> CalculateTimeBasedStatisticsAsync(List<Booking> bookings, DateTime fromDate, DateTime toDate, string groupBy, bool includeComparison, List<int> partnerCinemaIds)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var thisYearStart = new DateTime(today.Year, 1, 1);

            var todayStats = CalculateTimePeriodStat(paidBookings, today, today.AddDays(1));
            var yesterdayStats = CalculateTimePeriodStat(paidBookings, yesterday, today);
            var thisWeekStats = CalculateTimePeriodStat(paidBookings, thisWeekStart, today.AddDays(1));
            var thisMonthStats = CalculateTimePeriodStat(paidBookings, thisMonthStart, today.AddDays(1));
            var thisYearStats = CalculateTimePeriodStat(paidBookings, thisYearStart, today.AddDays(1));

            var revenueTrend = CalculateRevenueTrend(paidBookings, fromDate, toDate, groupBy);

            PartnerResponses.PeriodComparison? periodComparison = null;
            if (includeComparison)
            {
                var periodDays = (toDate - fromDate).Days;
                var previousFromDate = fromDate.AddDays(-periodDays - 1);
                var previousToDate = fromDate.AddTicks(-1);

                var previousBookingsRaw = await _context.Bookings
                    .Include(b => b.Tickets)
                    .AsNoTracking()
                    .Where(b => partnerCinemaIds.Contains(b.Showtime.CinemaId))
                    .Where(b => b.BookingTime >= previousFromDate && b.BookingTime <= previousToDate)
                    .ToListAsync();
                
                var previousBookings = previousBookingsRaw.Where(b => IsPaidBooking(b)).ToList();

                var currentPeriod = new PartnerResponses.PeriodData
                {
                    Revenue = paidBookings.Sum(b => b.TotalAmount),
                    Bookings = paidBookings.Count,
                    Customers = paidBookings.Select(b => b.CustomerId).Distinct().Count()
                };

                var previousPeriod = new PartnerResponses.PeriodData
                {
                    Revenue = previousBookings.Sum(b => b.TotalAmount),
                    Bookings = previousBookings.Count,
                    Customers = previousBookings.Select(b => b.CustomerId).Distinct().Count()
                };

                periodComparison = new PartnerResponses.PeriodComparison
                {
                    CurrentPeriod = currentPeriod,
                    PreviousPeriod = previousPeriod,
                    Growth = new PartnerResponses.GrowthData
                    {
                        RevenueGrowth = previousPeriod.Revenue > 0
                            ? (decimal)(((double)(currentPeriod.Revenue - previousPeriod.Revenue) / (double)previousPeriod.Revenue) * 100)
                            : (currentPeriod.Revenue > 0 ? 100 : 0),
                        BookingGrowth = previousPeriod.Bookings > 0
                            ? (decimal)(((currentPeriod.Bookings - previousPeriod.Bookings) / (double)previousPeriod.Bookings) * 100)
                            : (currentPeriod.Bookings > 0 ? 100 : 0),
                        CustomerGrowth = previousPeriod.Customers > 0
                            ? (decimal)(((currentPeriod.Customers - previousPeriod.Customers) / (double)previousPeriod.Customers) * 100)
                            : (currentPeriod.Customers > 0 ? 100 : 0)
                    }
                };
            }

            return new PartnerResponses.TimeBasedStatistics
            {
                Today = todayStats,
                Yesterday = yesterdayStats,
                ThisWeek = thisWeekStats,
                ThisMonth = thisMonthStats,
                ThisYear = thisYearStats,
                RevenueTrend = revenueTrend,
                PeriodComparison = periodComparison
            };
        }

        private PartnerResponses.TimePeriodStat CalculateTimePeriodStat(List<Booking> bookings, DateTime startDate, DateTime endDate)
        {
            var periodBookings = bookings.Where(b => b.BookingTime >= startDate && b.BookingTime < endDate).ToList();

            return new PartnerResponses.TimePeriodStat
            {
                Bookings = periodBookings.Count,
                Revenue = periodBookings.Sum(b => b.TotalAmount),
                Tickets = periodBookings.Sum(b => b.Tickets.Count),
                Customers = periodBookings.Select(b => b.CustomerId).Distinct().Count()
            };
        }

        private List<PartnerResponses.TimeSeriesData> CalculateRevenueTrend(List<Booking> bookings, DateTime fromDate, DateTime toDate, string groupBy)
        {
            var trend = new List<PartnerResponses.TimeSeriesData>();

            switch (groupBy.ToLower())
            {
                case "day":
                    var dailyGroups = bookings
                        .GroupBy(b => b.BookingTime.Date)
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = dailyGroups.Select(g => new PartnerResponses.TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;

                case "week":
                    var weeklyGroups = bookings
                        .GroupBy(b => GetWeekStart(b.BookingTime))
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = weeklyGroups.Select(g => new PartnerResponses.TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;

                case "month":
                    var monthlyGroups = bookings
                        .GroupBy(b => new DateTime(b.BookingTime.Year, b.BookingTime.Month, 1))
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = monthlyGroups.Select(g => new PartnerResponses.TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy-MM"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;

                case "year":
                    var yearlyGroups = bookings
                        .GroupBy(b => new DateTime(b.BookingTime.Year, 1, 1))
                        .OrderBy(g => g.Key)
                        .ToList();

                    trend = yearlyGroups.Select(g => new PartnerResponses.TimeSeriesData
                    {
                        Date = g.Key.ToString("yyyy"),
                        Revenue = g.Sum(b => b.TotalAmount),
                        BookingCount = g.Count(),
                        TicketCount = g.Sum(b => b.Tickets.Count)
                    }).ToList();
                    break;
            }

            return trend;
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private PartnerResponses.TopCustomersStatistics CalculateTopCustomersStatistics(List<Booking> bookings, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            var customerStats = paidBookings
                .GroupBy(b => new
                {
                    b.CustomerId,
                    UserId = b.Customer.User.UserId,
                    Fullname = b.Customer.User.Fullname,
                    Email = b.Customer.User.Email,
                    Phone = b.Customer.User.Phone
                })
                .Select(g => new PartnerResponses.CustomerStat
                {
                    CustomerId = g.Key.CustomerId,
                    UserId = g.Key.UserId,
                    Fullname = g.Key.Fullname,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    TotalSpent = g.Sum(b => b.TotalAmount),
                    TotalBookings = g.Count(),
                    TotalTicketsPurchased = g.Sum(b => b.Tickets.Count),
                    AverageOrderValue = g.Average(b => b.TotalAmount),
                    LastBookingDate = g.Max(b => b.BookingTime)
                })
                .ToList();

            var customersByRevenue = customerStats
                .OrderByDescending(c => c.TotalSpent)
                .ToList();

            var customersByBookingCount = customerStats
                .OrderByDescending(c => c.TotalBookings)
                .ToList();

            var topByRevenue = customersByRevenue.Take(topLimit).ToList();
            var topByBookingCount = customersByBookingCount.Take(topLimit).ToList();

            // Pagination for revenue list
            var totalCountByRevenue = customersByRevenue.Count;
            var totalPagesByRevenue = (int)Math.Ceiling(totalCountByRevenue / (double)pageSize);
            var paginatedByRevenue = customersByRevenue
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pagination for booking count list
            var totalCountByBookingCount = customersByBookingCount.Count;
            var totalPagesByBookingCount = (int)Math.Ceiling(totalCountByBookingCount / (double)pageSize);
            var paginatedByBookingCount = customersByBookingCount
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PartnerResponses.TopCustomersStatistics
            {
                ByRevenue = paginatedByRevenue,
                ByBookingCount = paginatedByBookingCount,
                PaginationByRevenue = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCountByRevenue,
                    TotalPages = totalPagesByRevenue
                },
                PaginationByBookingCount = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCountByBookingCount,
                    TotalPages = totalPagesByBookingCount
                }
            };
        }

        private PartnerResponses.ServiceStatistics CalculateServiceStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var totalRevenue = paidBookings.Sum(b => b.TotalAmount);
            var totalServiceRevenue = paidBookings.Sum(b => b.ServiceOrders.Sum(so => so.Quantity * so.UnitPrice));
            var totalServiceOrders = paidBookings.Sum(b => b.ServiceOrders.Count);
            var serviceRevenuePercentage = totalRevenue > 0 ? (totalServiceRevenue / totalRevenue) * 100 : 0;

            var topServices = paidBookings
                .SelectMany(b => b.ServiceOrders)
                .GroupBy(so => new
                {
                    so.ServiceId,
                    ServiceName = so.Service.ServiceName
                })
                .Select(g => new PartnerResponses.TopServiceStat
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.ServiceName,
                    TotalQuantity = g.Sum(so => so.Quantity),
                    TotalRevenue = g.Sum(so => so.Quantity * so.UnitPrice),
                    BookingCount = g.Select(so => so.BookingId).Distinct().Count()
                })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(10)
                .ToList();

            return new PartnerResponses.ServiceStatistics
            {
                TotalServiceRevenue = totalServiceRevenue,
                TotalServiceOrders = totalServiceOrders,
                ServiceRevenuePercentage = serviceRevenuePercentage,
                TopServices = topServices
            };
        }

        private async Task<PartnerResponses.SeatStatistics> CalculateSeatStatisticsAsync(List<Booking> bookings, List<int> partnerCinemaIds)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var totalSeatsSold = paidBookings.Sum(b => b.Tickets.Count);

            // Get total available seats from all screens in partner's cinemas
            var totalSeatsAvailable = await _context.Screens
                .Where(s => partnerCinemaIds.Contains(s.CinemaId) && s.IsActive)
                .SumAsync(s => s.Capacity ?? 0);

            var overallOccupancyRate = totalSeatsAvailable > 0 
                ? (totalSeatsSold / (decimal)totalSeatsAvailable) * 100 
                : 0;

            // Statistics by seat type
            var seatTypeStats = paidBookings
                .SelectMany(b => b.Tickets)
                .Where(t => t.Seat.SeatType != null)
                .GroupBy(t => new
                {
                    SeatTypeId = t.Seat.SeatType!.Id,
                    SeatTypeName = t.Seat.SeatType.Name
                })
                .Select(g => new PartnerResponses.SeatTypeStat
                {
                    SeatTypeId = g.Key.SeatTypeId,
                    SeatTypeName = g.Key.SeatTypeName,
                    TotalTicketsSold = g.Count(),
                    TotalRevenue = g.Sum(t => t.Price),
                    AveragePrice = g.Average(t => t.Price)
                })
                .OrderByDescending(s => s.TotalTicketsSold)
                .ToList();

            return new PartnerResponses.SeatStatistics
            {
                TotalSeatsSold = totalSeatsSold,
                TotalSeatsAvailable = totalSeatsAvailable,
                OverallOccupancyRate = overallOccupancyRate,
                BySeatType = seatTypeStats
            };
        }

        private async Task<PartnerResponses.ShowtimeStatistics> CalculateShowtimeStatisticsAsync(List<Booking> bookings, List<int> partnerCinemaIds, int topLimit, int page, int pageSize)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();

            // Get all showtimes from partner's cinemas in the date range
            var fromDate = bookings.Any() ? bookings.Min(b => b.BookingTime).Date : DateTime.UtcNow.AddDays(-30).Date;
            var toDate = bookings.Any() ? bookings.Max(b => b.BookingTime).Date.AddDays(1) : DateTime.UtcNow.Date.AddDays(1);

            var allShowtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .AsNoTracking()
                .Where(s => partnerCinemaIds.Contains(s.CinemaId))
                .Where(s => s.ShowDatetime >= fromDate && s.ShowDatetime <= toDate)
                .ToListAsync();

            var totalShowtimes = allShowtimes.Count;
            var showtimesWithBookings = paidBookings.Select(b => b.ShowtimeId).Distinct().Count();
            var showtimesWithoutBookings = totalShowtimes - showtimesWithBookings;

            // Top showtimes by revenue
            var showtimesByRevenue = paidBookings
                .GroupBy(b => new
                {
                    b.ShowtimeId,
                    ShowDatetime = b.Showtime.ShowDatetime,
                    FormatType = b.Showtime.FormatType,
                    MovieTitle = b.Showtime.Movie.Title,
                    CinemaName = b.Showtime.Cinema.CinemaName,
                    ScreenCapacity = b.Showtime.Screen.Capacity ?? 0
                })
                .Select(g => new PartnerResponses.TopShowtimeStat
                {
                    ShowtimeId = g.Key.ShowtimeId,
                    ShowDatetime = g.Key.ShowDatetime,
                    FormatType = g.Key.FormatType,
                    MovieTitle = g.Key.MovieTitle,
                    CinemaName = g.Key.CinemaName ?? "",
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TotalTicketsSold = g.Sum(b => b.Tickets.Count),
                    OccupancyRate = g.Key.ScreenCapacity > 0 
                        ? (g.Sum(b => b.Tickets.Count) / (decimal)g.Key.ScreenCapacity) * 100 
                        : 0
                })
                .OrderByDescending(s => s.TotalRevenue)
                .ToList();

            var topShowtimes = showtimesByRevenue.Take(topLimit).ToList();

            // Pagination
            var totalCount = showtimesByRevenue.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var paginatedShowtimes = showtimesByRevenue
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PartnerResponses.ShowtimeStatistics
            {
                TotalShowtimes = totalShowtimes,
                ShowtimesWithBookings = showtimesWithBookings,
                ShowtimesWithoutBookings = showtimesWithoutBookings,
                TopShowtimesByRevenue = paginatedShowtimes,
                Pagination = new ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses.PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                }
            };
        }

        private PartnerResponses.PaymentStatistics CalculatePaymentStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var totalBookings = bookings.Count;
            var failedBookings = bookings.Count(b => 
                b.PaymentStatus?.ToUpper() == "FAILED" || 
                b.Status.ToUpper() == "FAILED");

            var failedPaymentRate = totalBookings > 0 
                ? (failedBookings / (double)totalBookings) * 100 
                : 0;

            var pendingPaymentAmount = bookings
                .Where(b => b.PaymentStatus?.ToUpper() == "PENDING" || b.Status.ToUpper() == "PENDING_PAYMENT")
                .Sum(b => b.TotalAmount);

            var paymentByProvider = bookings
                .Where(b => !string.IsNullOrEmpty(b.PaymentProvider))
                .GroupBy(b => b.PaymentProvider!)
                .Select(g => new PartnerResponses.PaymentProviderStat
                {
                    Provider = g.Key,
                    BookingCount = g.Count(),
                    TotalAmount = g.Where(b => IsPaidBooking(b)).Sum(b => b.TotalAmount)
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            return new PartnerResponses.PaymentStatistics
            {
                PaymentByProvider = paymentByProvider,
                FailedPaymentRate = (decimal)failedPaymentRate,
                PendingPaymentAmount = pendingPaymentAmount
            };
        }

        private PartnerResponses.VoucherStatistics CalculateVoucherStatistics(List<Booking> bookings)
        {
            var paidBookings = bookings.Where(b => IsPaidBooking(b)).ToList();
            var bookingsWithVoucher = paidBookings.Where(b => b.Voucher != null).ToList();

            var totalVouchersUsed = bookingsWithVoucher.Count;
            var totalVoucherDiscount = bookingsWithVoucher.Sum(b =>
            {
                if (b.Voucher == null) return 0;
                if (b.Voucher.DiscountType.ToLower() == "fixed")
                    return b.Voucher.DiscountVal;
                else // percent
                    return b.TotalAmount * (b.Voucher.DiscountVal / 100);
            });

            var voucherUsageRate = paidBookings.Any()
                ? (totalVouchersUsed / (double)paidBookings.Count) * 100
                : 0;

            var mostUsedVouchers = bookingsWithVoucher
                .Where(b => b.Voucher != null)
                .GroupBy(b => b.Voucher!.VoucherCode)
                .Select(g => new PartnerResponses.VoucherUsageStat
                {
                    VoucherCode = g.Key,
                    UsageCount = g.Count(),
                    TotalDiscount = g.Sum(b =>
                    {
                        var v = b.Voucher!;
                        if (v.DiscountType.ToLower() == "fixed")
                            return v.DiscountVal;
                        else
                            return b.TotalAmount * (v.DiscountVal / 100);
                    })
                })
                .OrderByDescending(v => v.UsageCount)
                .Take(10)
                .ToList();

            return new PartnerResponses.VoucherStatistics
            {
                TotalVouchersUsed = totalVouchersUsed,
                TotalVoucherDiscount = totalVoucherDiscount,
                VoucherUsageRate = (decimal)voucherUsageRate,
                MostUsedVouchers = mostUsedVouchers
            };
        }
     }
 }
