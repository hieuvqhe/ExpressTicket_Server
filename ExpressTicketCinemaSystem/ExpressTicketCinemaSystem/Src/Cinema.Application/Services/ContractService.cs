using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests.ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ContractService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IEmailService _emailService;
        private readonly IManagerService _managerService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly ILogger<ContractService> _logger;

        public ContractService(CinemaDbCoreContext context , IEmailService emailService , IManagerService managerService , IAzureBlobService azureBlobService, ILogger<ContractService> logger)
        {
            _context = context;
            _emailService = emailService;
            _managerService = managerService;
            _azureBlobService = azureBlobService;
            _logger = logger;
        }

        public async Task<ContractResponse> CreateContractDraftAsync(int managerId, CreateContractRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateRequiredFields(request);
            await ValidatePartnerAsync(request.PartnerId);
            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }
            ValidateContractDates(request.StartDate, request.EndDate);
            ValidateCommissionRate(request.CommissionRate);
            await ValidateContractNumberAsync(request.ContractNumber);
            ValidateContractType(request.ContractType);

            // ==================== BUSINESS LOGIC SECTION ====================

            var contractHash = GenerateContractHash(request);

            var contract = new Contract
            {
                ManagerId = managerId,
                PartnerId = request.PartnerId,
                ContractNumber = request.ContractNumber,
                ContractType = request.ContractType,
                Title = request.Title,
                Description = request.Description,
                TermsAndConditions = request.TermsAndConditions,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CommissionRate = request.CommissionRate,
                MinimumRevenue = request.MinimumRevenue,
                Status = "draft",
                IsLocked = false,
                ContractHash = contractHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = managerId
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            // Lấy thông tin đầy đủ cho PDF generation
            var result = await GetContractWithFullDetailsAsync(contract.ContractId);

            return result;
        }

        private string GenerateContractHash(CreateContractRequest request)
        {
            var content = $"{request.ContractNumber}{request.Title}{request.TermsAndConditions}" +
                          $"{request.StartDate:yyyy-MM-dd}{request.EndDate:yyyy-MM-dd}" +
                          $"{request.CommissionRate}{request.MinimumRevenue}{DateTime.UtcNow.Ticks}";

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(bytes);
        }
        public async Task SendContractPdfToPartnerAsync(int contractId, int managerId, SendContractPdfRequest request)
        {
            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");

            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }
            if (contract.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền gửi hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            ValidateContractForSending(contract);

            // ==================== BUSINESS LOGIC SECTION ====================

            contract.Status = "pending_signature";
            contract.PdfUrl = request.PdfUrl; 
            contract.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            try
            {
                // 2. TẠO SAS URL (DÒNG NÀY ĐANG GÂY LỖI 500)
                _logger.LogInformation("Đang tạo SAS Read URL cho: {PdfUrl}", request.PdfUrl);

                string readableSasUrl = await _azureBlobService.GeneratePdfReadUrlAsync(request.PdfUrl, expiryInDays: 7);

                // 3. GỬI EMAIL
                _logger.LogInformation("Đã tạo SAS thành công, chuẩn bị gửi email.");
                await SendContractPdfEmailAsync(contract, readableSasUrl, request.Notes);
            }
            catch (Exception ex)
               {
                // 4. LỖI GỐC SẼ ĐƯỢC IN RA Ở ĐÂY
                _logger.LogError(ex, "LỖI GỐC KHI TẠO SAS URL. PdfUrl: {PdfUrl}", request.PdfUrl);

                // DÙNG TẠM CÁI NÀY NẾU BẠN CHƯA CÓ LOGGER
                Console.WriteLine($"LỖI GỐC KHI TẠO SAS: {ex.ToString()}");
            }
        }
        
        private void ValidateContractForSending(Contract contract)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (contract.Status != "draft")
                errors["status"] = new ValidationError
                {
                    Msg = $"Chỉ có thể gửi hợp đồng với trạng thái 'draft'. Hiện tại: {contract.Status}",
                    Path = "status"
                };

            if (contract.IsLocked)
                errors["contract"] = new ValidationError
                {
                    Msg = "Hợp đồng đã được khóa, không thể gửi",
                    Path = "contract"
                };

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private async Task SendContractPdfEmailAsync(Contract contract, string pdfUrl, string? notes)
        {
            try
            {
                if (contract.Partner?.User?.Email != null)
                {
                    var subject = "HỢP ĐỒNG HỢP TÁC - CHỜ KÝ DUYỆT";

                    var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%); padding: 30px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9;'>
        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #2563eb;'>
            <p style='margin-bottom: 10px;'>Kính gửi Ông/Bà <strong>{contract.Partner.User.Fullname}</strong>,</p>
            <p style='margin-bottom: 20px;'>Hợp đồng hợp tác đã được soạn thảo và đang chờ Quý đối tác ký duyệt.</p>
            
            <h4 style='color: #333; margin-bottom: 15px;'>THÔNG TIN HỢP ĐỒNG:</h4>
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666; width: 160px;'>Số hợp đồng:</td>
                        <td style='padding: 8px 0;'><strong>{contract.ContractNumber}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tiêu đề:</td>
                        <td style='padding: 8px 0;'><strong>{contract.Title}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Loại hợp đồng:</td>
                        <td style='padding: 8px 0;'><strong>{contract.ContractType}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Ngày bắt đầu:</td>
                        <td style='padding: 8px 0;'><strong>{contract.StartDate:dd/MM/yyyy}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Ngày kết thúc:</td>
                        <td style='padding: 8px 0;'><strong>{contract.EndDate:dd/MM/yyyy}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tỷ lệ hoa hồng:</td>
                        <td style='padding: 8px 0;'><strong style='color: #2563eb;'>{contract.CommissionRate}%</strong></td>
                    </tr>
                </table>
            </div>
            
            <div style='background: #e0e7ff; padding: 20px; border-radius: 5px; margin-bottom: 20px; border: 1px solid #c7d2fe;'>
                <h4 style='color: #1e40af; margin: 0 0 15px 0;'>📄 LINK TẢI HỢP ĐỒNG PDF:</h4>
                <div style='text-align: center; margin: 15px 0;'>
                    <a href='{pdfUrl}' style='
                        display: inline-block;
                        background-color: #2563eb;
                        color: white;
                        padding: 12px 30px;
                        text-decoration: none;
                        border-radius: 5px;
                        font-weight: bold;
                        font-size: 16px;
                    '>Tải xuống hợp đồng PDF</a>
                </div>
                <p style='margin: 10px 0 0 0; color: #1e40af; font-size: 12px; word-break: break-all;'>
                    Hoặc copy link: <a href='{pdfUrl}' style='color: #2563eb;'>{pdfUrl}</a>
                </p>
            </div>
            
            <div style='background: #fff7ed; padding: 20px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #f59e0b;'>
                <h4 style='color: #92400e; margin: 0 0 15px 0;'>✍️ HƯỚNG DẪN KÝ:</h4>
                <ol style='color: #92400e; line-height: 1.8; margin: 0; padding-left: 20px;'>
                    <li>Tải file PDF từ link trên</li>
                    <li>In hợp đồng ra giấy</li>
                    <li>Ký tay và đóng dấu (nếu có)</li>
                    <li>Scan hợp đồng đã ký thành file PDF</li>
                    <li>Upload file PDF đã ký lên hệ thống</li>
                </ol>
            </div>
            
            {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
            <div style='background: #fef3c7; padding: 15px; border-radius: 5px; margin-bottom: 20px; border: 1px solid #fde68a;'>
                <h4 style='color: #78350f; margin: 0 0 10px 0;'>📌 GHI CHÚ:</h4>
                <p style='margin: 0; color: #78350f; line-height: 1.6;'>{WebUtility.HtmlEncode(notes)}</p>
            </div>
            ")}
            
            <div style='background: #fef2f2; padding: 15px; border-radius: 5px; border: 1px solid #fecaca;'>
                <p style='margin: 0; color: #991b1b; line-height: 1.6;'>
                    <strong>⏰ Lưu ý quan trọng:</strong><br>
                    Thực hiện ký kết hợp đồng trong vòng <strong>3 ngày</strong> và gửi lại qua hệ thống.
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
        <p style='margin: 10px 0 0 0; font-size: 12px; opacity: 0.9;'>
            Trân trọng,<br>
            <strong>{GetCompanyInfo().Name}</strong>
        </p>
    </div>
</div>";

                    await _emailService.SendEmailAsync(contract.Partner.User.Email, subject, htmlBody);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng đến business logic chính
                // GIỐNG HỆT cách xử lý trong SendContractFinalizedEmailAsync
                Console.WriteLine($"Failed to send contract PDF email: {ex.Message}");
            }
        }
        private void ValidateRequiredFields(CreateContractRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (request.PartnerId <= 0)
                errors["partnerId"] = new ValidationError { Msg = "Partner ID là bắt buộc", Path = "partnerId" };

            if (string.IsNullOrWhiteSpace(request.ContractNumber))
                errors["contractNumber"] = new ValidationError { Msg = "Số hợp đồng là bắt buộc", Path = "contractNumber" };

            if (string.IsNullOrWhiteSpace(request.ContractType))
                errors["contractType"] = new ValidationError { Msg = "Loại hợp đồng là bắt buộc", Path = "contractType" };

            if (string.IsNullOrWhiteSpace(request.Title))
                errors["title"] = new ValidationError { Msg = "Tiêu đề hợp đồng là bắt buộc", Path = "title" };

            if (string.IsNullOrWhiteSpace(request.TermsAndConditions))
                errors["termsAndConditions"] = new ValidationError { Msg = "Điều khoản hợp đồng là bắt buộc", Path = "termsAndConditions" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidatePartnerAsync(int partnerId)
        {
            var partner = await _context.Partners
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner với ID này.");

            if (partner.Status != "approved")
                throw new ValidationException("partnerId", "Partner chưa được duyệt, không thể tạo hợp đồng.");
        }

        private void ValidateContractDates(DateTime startDate, DateTime endDate)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (startDate < DateTime.UtcNow.Date)
                errors["startDate"] = new ValidationError { Msg = "Ngày bắt đầu không thể trong quá khứ", Path = "startDate" };

            if (endDate <= startDate)
                errors["endDate"] = new ValidationError { Msg = "Ngày kết thúc phải sau ngày bắt đầu", Path = "endDate" };

            if (endDate.Subtract(startDate).TotalDays < 30)
                errors["endDate"] = new ValidationError { Msg = "Hợp đồng phải có thời hạn ít nhất 30 ngày", Path = "endDate" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateCommissionRate(decimal commissionRate)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (commissionRate <= 0)
                errors["commissionRate"] = new ValidationError { Msg = "Tỷ lệ hoa hồng phải lớn hơn 0", Path = "commissionRate" };
            else if (commissionRate > 50)
                errors["commissionRate"] = new ValidationError { Msg = "Tỷ lệ hoa hồng không thể vượt quá 50%", Path = "commissionRate" };

            if (decimal.Round(commissionRate, 2) != commissionRate)
                errors["commissionRate"] = new ValidationError { Msg = "Tỷ lệ hoa hồng chỉ được có tối đa 2 chữ số thập phân", Path = "commissionRate" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task ValidateContractNumberAsync(string contractNumber)
        {
            if (await _context.Contracts.AnyAsync(c => c.ContractNumber == contractNumber))
                throw new ConflictException("contractNumber", "Số hợp đồng đã tồn tại trong hệ thống");
        }

        private void ValidateContractType(string contractType)
        {
            var validTypes = new[] { "partnership", "service", "standard", "premium" };

            if (!validTypes.Contains(contractType.ToLower()))
                throw new ValidationException("contractType", $"Loại hợp đồng không hợp lệ. Các loại hợp lệ: {string.Join(", ", validTypes)}");
        }

        private async Task<ContractResponse> GetContractWithFullDetailsAsync(int contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng.");

            var companyInfo = GetCompanyInfo();

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                ContractType = contract.ContractType,
                Title = contract.Title,
                Description = contract.Description ?? "",
                TermsAndConditions = contract.TermsAndConditions,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CommissionRate = contract.CommissionRate,
                MinimumRevenue = contract.MinimumRevenue,
                Status = contract.Status,
                ContractHash = contract.ContractHash,
                PdfUrl = contract.PdfUrl,
                PartnerSignatureUrl = contract.PartnerSignatureUrl,
                ManagerSignature = contract.ManagerSignature,
                SignedAt = contract.SignedAt,
                PartnerSignedAt = contract.PartnerSignedAt,
                ManagerSignedAt = contract.ManagerSignedAt,
                LockedAt = contract.LockedAt,
                IsLocked = contract.IsLocked,
                ManagerId = contract.ManagerId,
                PartnerId = contract.PartnerId,
                CreatedBy = contract.CreatedBy,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,

                PartnerName = contract.Partner?.PartnerName ?? "",
                PartnerAddress = contract.Partner?.Address ?? "",
                PartnerTaxCode = contract.Partner?.TaxCode ?? "",
                PartnerRepresentative = contract.Partner?.User?.Fullname ?? "",
                PartnerPosition = "Đại diện hợp pháp",
                PartnerEmail = contract.Partner?.User?.Email ?? "",
                PartnerPhone = contract.Partner?.User?.Phone ?? "",

                CompanyName = companyInfo.Name,
                CompanyAddress = companyInfo.Address,
                CompanyTaxCode = companyInfo.TaxCode,
                ManagerName = contract.Manager?.User?.Fullname ?? "",
                ManagerPosition = "Quản lý Đối tác",
                ManagerEmail = contract.Manager?.User?.Email ?? "",
                CreatedByName = contract.Manager?.User?.Fullname ?? ""
            };
        }
        public async Task<PaginatedContractsResponse> GetAllContractsAsync(
    int currentManagerId,
    int page = 1,
    int limit = 10,
    int? managerId = null,
    int? partnerId = null,
    string? status = null,
    string? search = null,
    string? sortBy = "created_at",
    string? sortOrder = "desc")
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;
            var managerExists = await _managerService.ValidateManagerExistsAsync(currentManagerId);
            if (!managerExists)
            {
                currentManagerId = await _managerService.GetDefaultManagerIdAsync();
            }
            var query = _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .AsQueryable();

            if (managerId.HasValue)
            {
                var filterManagerExists = await _managerService.ValidateManagerExistsAsync(managerId.Value);
                if (!filterManagerExists)
                {
                    managerId = await _managerService.GetDefaultManagerIdAsync();
                }

                query = query.Where(c => c.ManagerId == managerId.Value);
            }
            else
            {
                query = query.Where(c => c.ManagerId == currentManagerId);
            }

            if (partnerId.HasValue)
                query = query.Where(c => c.PartnerId == partnerId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(c => c.Status == "active" || c.Status == "pending" || c.Status == "pending_signature");
                }
                else
                {
                    query = query.Where(c => c.Status == status);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.ContractNumber.ToLower().Contains(search) ||
                    c.Title.ToLower().Contains(search) ||
                    c.Partner.PartnerName.ToLower().Contains(search) ||
                    c.Partner.User.Fullname.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, sortBy, sortOrder);

            var contracts = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new ContractListResponse
                {
                    ContractId = c.ContractId,
                    ContractNumber = c.ContractNumber,
                    Title = c.Title,
                    ContractType = c.ContractType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CommissionRate = c.CommissionRate,
                    Status = c.Status,
                    IsLocked = c.IsLocked,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,

                    PartnerId = c.PartnerId,
                    PartnerName = c.Partner.PartnerName,
                    PartnerEmail = c.Partner.User.Email,
                    PartnerPhone = c.Partner.User.Phone,

                    ManagerId = c.ManagerId,
                    ManagerName = c.Manager.User.Fullname,

                    PartnerSignatureUrl = c.PartnerSignatureUrl
                })
                .ToListAsync();

            // Create pagination metadata
            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedContractsResponse
            {
                Contracts = contracts,
                Pagination = pagination
            };
        }

        private IQueryable<Contract> ApplySorting(IQueryable<Contract> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "created_at";
            sortOrder = sortOrder?.ToLower() ?? "desc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "contract_number" => isAscending ? query.OrderBy(c => c.ContractNumber) : query.OrderByDescending(c => c.ContractNumber),
                "title" => isAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
                "start_date" => isAscending ? query.OrderBy(c => c.StartDate) : query.OrderByDescending(c => c.StartDate),
                "end_date" => isAscending ? query.OrderBy(c => c.EndDate) : query.OrderByDescending(c => c.EndDate),
                "commission_rate" => isAscending ? query.OrderBy(c => c.CommissionRate) : query.OrderByDescending(c => c.CommissionRate),
                "status" => isAscending ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
                "updated_at" => isAscending ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
                "partner_name" => isAscending ? query.OrderBy(c => c.Partner.PartnerName) : query.OrderByDescending(c => c.Partner.PartnerName),
                _ => isAscending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt) // default
            };
        }
        public async Task<ContractResponse> GetContractByIdAsync(int contractId, int managerId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");
            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }
            // Security check: Ensure manager can only access their contracts
            if (contract.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền truy cập hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            var companyInfo = GetCompanyInfo();

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                ContractType = contract.ContractType,
                Title = contract.Title,
                Description = contract.Description ?? "",
                TermsAndConditions = contract.TermsAndConditions,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CommissionRate = contract.CommissionRate,
                MinimumRevenue = contract.MinimumRevenue,
                Status = contract.Status,
                IsLocked = contract.IsLocked,
                ContractHash = contract.ContractHash,
                PdfUrl = contract.PdfUrl,
                PartnerSignatureUrl = contract.PartnerSignatureUrl,
                ManagerSignature = contract.ManagerSignature,
                SignedAt = contract.SignedAt,
                PartnerSignedAt = contract.PartnerSignedAt,
                ManagerSignedAt = contract.ManagerSignedAt,
                LockedAt = contract.LockedAt,
                ManagerId = contract.ManagerId,
                PartnerId = contract.PartnerId,
                CreatedBy = contract.CreatedBy,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,

                // Thông tin partner cho PDF
                PartnerName = contract.Partner?.PartnerName ?? "",
                PartnerAddress = contract.Partner?.Address ?? "",
                PartnerTaxCode = contract.Partner?.TaxCode ?? "",
                PartnerRepresentative = contract.Partner?.User?.Fullname ?? "",
                PartnerPosition = "Đại diện hợp pháp",
                PartnerEmail = contract.Partner?.User?.Email ?? "",
                PartnerPhone = contract.Partner?.User?.Phone ?? "",

                // Thông tin manager
                ManagerName = contract.Manager?.User?.Fullname ?? "",
                ManagerPosition = "Quản lý Đối tác",
                ManagerEmail = contract.Manager?.User?.Email ?? "",

                // Thông tin người tạo
                CreatedByName = contract.Manager?.User?.Fullname ?? "",

                // Thông tin công ty cho PDF
                CompanyName = companyInfo.Name,
                CompanyAddress = companyInfo.Address,
                CompanyTaxCode = companyInfo.TaxCode
            };
        }
        public async Task<ContractResponse> FinalizeContractAsync(int contractId, int managerId, FinalizeContractRequest request)
        {
            // ==================== VALIDATION SECTION ====================

            ValidateFinalizeRequest(request);

            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");
            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }
            // Security check
            if (contract.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền thao tác với hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            // Business logic validation
            ValidateContractForFinalization(contract);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật thông tin hợp đồng
            // Cập nhật thông tin hợp đồng
            contract.Status = "active";
            contract.IsLocked = true;
            contract.IsActive = true;
            contract.ManagerSignature = request.ManagerSignature;
            contract.ManagerSignedAt = DateTime.UtcNow;
            contract.SignedAt = DateTime.UtcNow;
            contract.LockedAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            // QUAN TRỌNG: Update commission rate của partner theo contract
            if (contract.Partner != null)
            {
                contract.Partner.CommissionRate = contract.CommissionRate; // Lấy từ contract
                contract.Partner.UpdatedAt = DateTime.UtcNow;

                // Active partner account nếu chưa active
                if (!contract.Partner.IsActive)
                {
                    contract.Partner.IsActive = true;

                    // Active user account của partner
                    if (contract.Partner.User != null)
                    {
                        contract.Partner.User.IsActive = true;
                        contract.Partner.User.EmailConfirmed = true;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Lấy thông tin đầy đủ để trả về response
            var result = await GetContractWithFullDetailsAsync(contract.ContractId);

            // Gửi email thông báo cho partner
            await SendContractFinalizedEmailAsync(contract);

            return result;
        }

        private void ValidateFinalizeRequest(FinalizeContractRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.ManagerSignature))
                errors["managerSignature"] = new ValidationError { Msg = "Chữ ký số của manager là bắt buộc", Path = "managerSignature" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateContractForFinalization(Contract contract)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (contract.IsLocked)
                errors["contract"] = new ValidationError { Msg = "Hợp đồng đã được khóa, không thể chỉnh sửa", Path = "contract" };

            if (contract.Status != "pending" && contract.Status != "draft")
                errors["status"] = new ValidationError { Msg = $"Không thể hoàn tất hợp đồng với trạng thái: {contract.Status}", Path = "status" };

            if (string.IsNullOrEmpty(contract.PartnerSignatureUrl))
                errors["partnerSignature"] = new ValidationError { Msg = "Partner chưa upload ảnh biên bản ký", Path = "partnerSignature" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task SendContractFinalizedEmailAsync(Contract contract)
        {
            try
            {
                if (contract.Partner?.User?.Email != null)
                {
                    var subject = "HỢP ĐỒNG ĐÃ ĐƯỢC KÍCH HOẠT";

                    var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 28px;'>🎬 TicketExpress</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Hệ thống đặt vé rạp chiếu phim</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9;'>
        <div style='text-align: center; margin-bottom: 20px;'>
            <div style='font-size: 48px; margin-bottom: 10px;'>✅</div>
            <h2 style='color: #10b981; margin-bottom: 10px;'>KÍCH HOẠT THÀNH CÔNG!</h2>
            <p style='color: #666; font-size: 18px;'>Hợp đồng đã được kích hoạt và sẵn sàng sử dụng</p>
        </div>
        
        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #10b981;'>
            <p style='margin-bottom: 10px;'>Kính gửi Ông/Bà <strong>{contract.Partner?.User?.Fullname}</strong>,</p>
            <p style='margin-bottom: 20px;'>Hợp đồng <strong>{contract.ContractNumber} - {contract.Title}</strong> đã được kích hoạt thành công.</p>
            
            <h4 style='color: #333; margin-bottom: 15px;'>THÔNG TIN HỢP ĐỒNG:</h4>
            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666; width: 160px;'>Số hợp đồng:</td>
                        <td style='padding: 8px 0;'><strong>{contract.ContractNumber}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Loại hợp đồng:</td>
                        <td style='padding: 8px 0;'><strong>{contract.ContractType}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Ngày bắt đầu:</td>
                        <td style='padding: 8px 0;'><strong>{contract.StartDate:dd/MM/yyyy}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Ngày kết thúc:</td>
                        <td style='padding: 8px 0;'><strong>{contract.EndDate:dd/MM/yyyy}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tỷ lệ hoa hồng:</td>
                        <td style='padding: 8px 0;'><strong style='color: #10b981;'>{contract.CommissionRate}%</strong></td>
                    </tr>
                </table>
            </div>
            
            <div style='background: #d1fae5; padding: 20px; border-radius: 5px; border: 1px solid #a7f3d0; margin-bottom: 20px;'>
                <h4 style='color: #065f46; margin: 0 0 15px 0;'>🎉 Bắt đầu ngay:</h4>
                <p style='margin: 0; color: #065f46; line-height: 1.8;'>
                    Từ thời điểm này, Quý đối tác có thể bắt đầu sử dụng hệ thống để quản lý rạp chiếu phim.
                </p>
            </div>
            
            <div style='background: #eff6ff; padding: 15px; border-radius: 5px; border-left: 4px solid #3b82f6;'>
                <h4 style='color: #1e40af; margin: 0 0 10px 0;'>💡 Các tính năng có sẵn:</h4>
                <ul style='color: #1e40af; line-height: 1.8; margin: 0; padding-left: 20px;'>
                    <li>Quản lý thông tin rạp chiếu phim</li>
                    <li>Tạo và quản lý lịch chiếu phim</li>
                    <li>Theo dõi doanh thu và báo cáo</li>
                    <li>Quản lý đặt vé và khách hàng</li>
                </ul>
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
        <p style='margin: 10px 0 0 0; font-size: 12px; opacity: 0.9;'>
            Trân trọng,<br>
            <strong>{GetCompanyInfo().Name}</strong>
        </p>
    </div>
</div>";

                    await _emailService.SendEmailAsync(contract.Partner.User.Email, subject, htmlBody);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng đến business logic chính
                Console.WriteLine($"Failed to send contract finalized email: {ex.Message}");
            }
        }
        public async Task<ContractResponse> UploadPartnerSignatureAsync(int contractId, int partnerId, UploadSignatureRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            ValidateUploadSignatureRequest(request);

            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");

            // Security check - chỉ partner sở hữu hợp đồng mới được upload
            if (contract.PartnerId != partnerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền upload signature cho hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }
            if (contract.Status != "pending_signature")
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["status"] = new ValidationError
                    {
                        Msg = $"Chỉ có thể upload signature cho hợp đồng với trạng thái 'pending_signature'. Hiện tại: {contract.Status}",
                        Path = "contract",
                        Location = "body"
                    }
                });
            }

            // Business logic validation
            ValidateContractForSignatureUpload(contract);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Thay thế PDF hợp đồng bằng PDF đã ký của partner
            // Manager sẽ xem PDF đã ký này thay vì PDF ban đầu
            contract.PdfUrl = request.SignedContractPdfUrl;
            
            // Cập nhật PartnerSignatureUrl để tương thích ngược
            contract.PartnerSignatureUrl = request.SignedContractPdfUrl;
            
            contract.PartnerSignedAt = DateTime.UtcNow;
            contract.Status = "pending"; 
            contract.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Lấy thông tin đầy đủ để trả về response
            var result = await GetContractWithFullDetailsAsync(contract.ContractId);

            // Gửi email thông báo cho manager
            await SendSignatureUploadedEmailAsync(contract);

            return result;
        }

        private void ValidateUploadSignatureRequest(UploadSignatureRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (string.IsNullOrWhiteSpace(request.SignedContractPdfUrl))
                errors["signedContractPdfUrl"] = new ValidationError { Msg = "URL PDF hợp đồng đã ký là bắt buộc", Path = "signedContractPdfUrl" };

            // Validate URL format
            if (!string.IsNullOrWhiteSpace(request.SignedContractPdfUrl) &&
                !Uri.TryCreate(request.SignedContractPdfUrl, UriKind.Absolute, out _))
            {
                errors["signedContractPdfUrl"] = new ValidationError { Msg = "URL PDF không hợp lệ", Path = "signedContractPdfUrl" };
            }

            // Validate file type - chỉ chấp nhận PDF
            if (!string.IsNullOrWhiteSpace(request.SignedContractPdfUrl))
            {
                var extension = Path.GetExtension(request.SignedContractPdfUrl.Split('?')[0]).ToLower();
                if (extension != ".pdf")
                {
                    errors["signedContractPdfUrl"] = new ValidationError { Msg = "Chỉ chấp nhận file PDF hợp đồng đã ký", Path = "signedContractPdfUrl" };
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateContractForSignatureUpload(Contract contract)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (contract.IsLocked)
                errors["contract"] = new ValidationError { Msg = "Hợp đồng đã được khóa, không thể upload signature", Path = "contract" };

            if (contract.Status == "expired" || contract.Status == "terminated")
                errors["status"] = new ValidationError { Msg = $"Không thể upload signature cho hợp đồng với trạng thái: {contract.Status}", Path = "status" };

            if (!string.IsNullOrEmpty(contract.PartnerSignatureUrl))
                errors["signature"] = new ValidationError { Msg = "Signature đã được upload trước đó", Path = "signature" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task SendSignatureUploadedEmailAsync(Contract contract)
        {
            try
            {
                if (contract.Manager?.User?.Email != null)
                {
                    var subject = "PARTNER ĐÃ UPLOAD BIÊN BẢN KÝ HỢP ĐỒNG";
                    var body = $"""
                Kính gửi Quản lý {contract.Manager.User.Fullname},

                Đối tác {contract.Partner?.PartnerName} đã upload biên bản ký cho hợp đồng:
                
                - Số hợp đồng: {contract.ContractNumber}
                - Tên hợp đồng: {contract.Title}
                - Đối tác: {contract.Partner?.PartnerName}
                - Thời gian upload: {DateTime.UtcNow:dd/MM/yyyy HH:mm}

                Vui lòng truy cập hệ thống để xem xét và hoàn tất hợp đồng.

                Trân trọng,
                Hệ thống Express Ticket Cinema
                """;

                    await _emailService.SendEmailAsync(contract.Manager.User.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw
                Console.WriteLine($"Failed to send signature uploaded email: {ex.Message}");
            }
        }
        public async Task<PaginatedContractsResponse> GetPartnerContractsAsync(
     int partnerId,
     int page = 1,
     int limit = 10,
     string? status = null,
     string? contractType = null,
     string? search = null,
     string? sortBy = "created_at",
     string? sortOrder = "desc")
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            // Base query - chỉ lấy contracts của partner hiện tại VÀ ĐÃ ĐƯỢC GỬI
            var query = _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .Where(c => c.PartnerId == partnerId &&
                           (c.Status == "pending_signature" || c.Status == "active" || c.Status == "expired")) // ← CHỈ HIỂN THỊ CÁC STATUS ĐÃ ĐƯỢC GỬI
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(contractType))
                query = query.Where(c => c.ContractType == contractType);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.ContractNumber.ToLower().Contains(search) ||
                    c.Title.ToLower().Contains(search) ||
                    c.Description.ToLower().Contains(search));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplyPartnerContractSorting(query, sortBy, sortOrder);

            // Apply pagination
            var contracts = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new ContractListResponse
                {
                    ContractId = c.ContractId,
                    ContractNumber = c.ContractNumber,
                    Title = c.Title,
                    ContractType = c.ContractType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CommissionRate = c.CommissionRate,
                    Status = c.Status,
                    IsLocked = c.IsLocked,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,

                    // Partner information (luôn là partner hiện tại)
                    PartnerId = c.PartnerId,
                    PartnerName = c.Partner.PartnerName,
                    PartnerEmail = c.Partner.User.Email,
                    PartnerPhone = c.Partner.User.Phone,

                    // Manager information
                    ManagerId = c.ManagerId,
                    ManagerName = c.Manager.User.Fullname,
                })
                .ToListAsync();

            // Create pagination metadata
            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedContractsResponse
            {
                Contracts = contracts,
                Pagination = pagination
            };
        }
        public async Task<Partner> GetPartnerByUserId(int userId)
        {
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner cho user ID này.");

            return partner;
        }
        private IQueryable<Contract> ApplyPartnerContractSorting(IQueryable<Contract> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "created_at";
            sortOrder = sortOrder?.ToLower() ?? "desc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "contract_number" => isAscending ? query.OrderBy(c => c.ContractNumber) : query.OrderByDescending(c => c.ContractNumber),
                "title" => isAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
                "start_date" => isAscending ? query.OrderBy(c => c.StartDate) : query.OrderByDescending(c => c.StartDate),
                "end_date" => isAscending ? query.OrderBy(c => c.EndDate) : query.OrderByDescending(c => c.EndDate),
                "commission_rate" => isAscending ? query.OrderBy(c => c.CommissionRate) : query.OrderByDescending(c => c.CommissionRate),
                "status" => isAscending ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
                "updated_at" => isAscending ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
                _ => isAscending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt) // default
            };
        }
        public async Task<ContractResponse> GetPartnerContractByIdAsync(int contractId, int userId)
        {
            // Tìm partner từ userId
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (partner == null)
                throw new NotFoundException("Không tìm thấy partner cho user này.");

            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");

            // Security check - chỉ partner sở hữu hợp đồng mới được xem
            if (contract.PartnerId != partner.PartnerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền xem hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }
            if (contract.Status == "draft")
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Không có hợp đồng này . Vui lòng kiểm tra lại",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            var companyInfo = GetCompanyInfo();

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                ManagerId = contract.ManagerId,
                PartnerId = contract.PartnerId,
                CreatedBy = contract.CreatedBy,
                ContractNumber = contract.ContractNumber,
                ContractType = contract.ContractType,
                Title = contract.Title,
                Description = contract.Description ?? "",
                TermsAndConditions = contract.TermsAndConditions,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CommissionRate = contract.CommissionRate,
                MinimumRevenue = contract.MinimumRevenue,
                Status = contract.Status,
                IsLocked = contract.IsLocked,
                ContractHash = contract.ContractHash,
                PartnerSignatureUrl = contract.PartnerSignatureUrl,
                ManagerSignature = contract.ManagerSignature,
                SignedAt = contract.SignedAt,
                PartnerSignedAt = contract.PartnerSignedAt,
                ManagerSignedAt = contract.ManagerSignedAt,
                LockedAt = contract.LockedAt,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,

                // Thông tin partner cho PDF
                PartnerName = contract.Partner?.PartnerName ?? "",
                PartnerAddress = contract.Partner?.Address ?? "",
                PartnerTaxCode = contract.Partner?.TaxCode ?? "",
                PartnerRepresentative = contract.Partner?.User?.Fullname ?? "",
                PartnerPosition = "Đại diện hợp pháp",
                PartnerEmail = contract.Partner?.User?.Email ?? "",
                PartnerPhone = contract.Partner?.User?.Phone ?? "",

                // Thông tin manager
                ManagerName = contract.Manager?.User?.Fullname ?? "",
                ManagerPosition = "Quản lý Đối tác",
                ManagerEmail = contract.Manager?.User?.Email ?? "",

                // Thông tin người tạo
                CreatedByName = contract.Manager?.User?.Fullname ??"",

                // Thông tin công ty cho PDF
                CompanyName = companyInfo.Name,
                CompanyAddress = companyInfo.Address,
                CompanyTaxCode = companyInfo.TaxCode
            };
        }
        public async Task<ContractResponse> UpdateContractDraftAsync(int contractId, int managerId, UpdateContractRequest request)
        {
            // ==================== VALIDATION SECTION ====================
            var contract = await _context.Contracts
                .Include(c => c.Partner)
                    .ThenInclude(p => p.User)
                .Include(c => c.Manager)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");

            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }

            // Security check
            if (contract.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền chỉnh sửa hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            // Business logic validation - chỉ cho phép sửa draft
            ValidateContractForUpdate(contract);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Cập nhật các field nếu có giá trị
            if (!string.IsNullOrWhiteSpace(request.ContractNumber) && request.ContractNumber != contract.ContractNumber)
            {
                await ValidateContractNumberAsync(request.ContractNumber);
                contract.ContractNumber = request.ContractNumber;
            }

            if (!string.IsNullOrWhiteSpace(request.ContractType))
            {
                ValidateContractType(request.ContractType);
                contract.ContractType = request.ContractType;
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
                contract.Title = request.Title;

            if (request.Description != null)
                contract.Description = request.Description;

            if (!string.IsNullOrWhiteSpace(request.TermsAndConditions))
                contract.TermsAndConditions = request.TermsAndConditions;

            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                var startDate = request.StartDate ?? contract.StartDate;
                var endDate = request.EndDate ?? contract.EndDate;
                ValidateContractDates(startDate, endDate);

                contract.StartDate = startDate;
                contract.EndDate = endDate;
            }

            if (request.CommissionRate.HasValue)
            {
                ValidateCommissionRate(request.CommissionRate.Value);
                contract.CommissionRate = request.CommissionRate.Value;
            }

            if (request.MinimumRevenue.HasValue)
                contract.MinimumRevenue = request.MinimumRevenue.Value;

            // Regenerate contract hash vì nội dung đã thay đổi
            contract.ContractHash = GenerateContractHash(new CreateContractRequest
            {
                ContractNumber = contract.ContractNumber,
                Title = contract.Title,
                TermsAndConditions = contract.TermsAndConditions,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CommissionRate = contract.CommissionRate,
                MinimumRevenue = contract.MinimumRevenue
            });

            contract.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetContractWithFullDetailsAsync(contract.ContractId);
        }

        public async Task CancelContractAsync(int contractId, int managerId)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                throw new NotFoundException("Không tìm thấy hợp đồng với ID này.");

            var managerExists = await _managerService.ValidateManagerExistsAsync(managerId);
            if (!managerExists)
            {
                managerId = await _managerService.GetDefaultManagerIdAsync();
            }

            // Security check
            if (contract.ManagerId != managerId)
            {
                throw new UnauthorizedException(new Dictionary<string, ValidationError>
                {
                    ["access"] = new ValidationError
                    {
                        Msg = "Bạn không có quyền hủy hợp đồng này",
                        Path = "contractId",
                        Location = "path"
                    }
                });
            }

            // Business logic validation - chỉ cho phép hủy draft
            ValidateContractForCancellation(contract);

            // ==================== BUSINESS LOGIC SECTION ====================

            // Soft delete: cập nhật status thành cancelled
            contract.Status = "cancelled";
            contract.IsLocked = true; // Khóa không cho chỉnh sửa
            contract.UpdatedAt = DateTime.UtcNow;
            contract.LockedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private void ValidateContractForUpdate(Contract contract)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (contract.Status != "draft")
                errors["status"] = new ValidationError
                {
                    Msg = $"Chỉ có thể chỉnh sửa hợp đồng với trạng thái 'draft'. Hiện tại: {contract.Status}",
                    Path = "status"
                };

            if (contract.IsLocked)
                errors["contract"] = new ValidationError
                {
                    Msg = "Hợp đồng đã được khóa, không thể chỉnh sửa",
                    Path = "contract"
                };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private void ValidateContractForCancellation(Contract contract)
        {
            var errors = new Dictionary<string, ValidationError>();

            if (contract.Status != "draft")
                errors["status"] = new ValidationError
                {
                    Msg = $"Chỉ có thể hủy hợp đồng với trạng thái 'draft'. Hiện tại: {contract.Status}",
                    Path = "status"
                };

            if (contract.IsLocked)
                errors["contract"] = new ValidationError
                {
                    Msg = "Hợp đồng đã được khóa, không thể hủy",
                    Path = "contract"
                };

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private CompanyInfo GetCompanyInfo()
        {
            // Trong thực tế, lấy từ database hoặc configuration
            return new CompanyInfo
            
            {
                Name = "CÔNG TY TNHH EXPRESS TICKET CINEMA SYSTEM",
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                TaxCode = "0312345678"
            };
        }

        private class CompanyInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string TaxCode { get; set; } = string.Empty;
        }
    }
}