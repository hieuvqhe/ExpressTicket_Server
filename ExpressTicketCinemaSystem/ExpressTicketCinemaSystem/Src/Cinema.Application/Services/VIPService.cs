using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IVIPService
    {
        /// <summary>
        /// Tích điểm cho customer khi thanh toán thành công
        /// </summary>
        Task AwardPointsForOrderAsync(int customerId, string orderId, decimal orderAmount, CancellationToken ct = default);

        /// <summary>
        /// Lấy thông tin VIP status của customer
        /// </summary>
        Task<VIPStatusResponse> GetVIPStatusAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Lấy lịch sử tích điểm
        /// </summary>
        Task<PointHistoryResponse> GetPointHistoryAsync(int customerId, int page = 1, int limit = 20, CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra và nâng cấp VIP level nếu đủ điều kiện
        /// </summary>
        Task<bool> CheckAndUpgradeVIPLevelAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Nhận quà nâng cấp VIP
        /// </summary>
        Task<ClaimBenefitResponse> ClaimUpgradeBonusAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Nhận quà sinh nhật
        /// </summary>
        Task<ClaimBenefitResponse> ClaimBirthdayBonusAsync(int customerId, CancellationToken ct = default);
    }

    public class VIPService : IVIPService
    {
        private readonly CinemaDbCoreContext _db;
        private readonly ILogger<VIPService> _logger;
        private readonly IVoucherService _voucherService;
        private readonly IManagerService _managerService;

        public VIPService(
            CinemaDbCoreContext db,
            ILogger<VIPService> logger,
            IVoucherService voucherService,
            IManagerService managerService)
        {
            _db = db;
            _logger = logger;
            _voucherService = voucherService;
            _managerService = managerService;
        }

        public async Task AwardPointsForOrderAsync(int customerId, string orderId, decimal orderAmount, CancellationToken ct = default)
        {
            try
            {
                // Kiểm tra xem đã tích điểm cho order này chưa
                var existingHistory = await _db.PointHistories
                    .FirstOrDefaultAsync(ph => ph.OrderId == orderId && ph.TransactionType == "EARNED", ct);

                if (existingHistory != null)
                {
                    _logger.LogWarning("Order {OrderId} đã được tích điểm rồi", orderId);
                    return;
                }

                // Lấy hoặc tạo VIPMember
                var vipMember = await GetOrCreateVIPMemberAsync(customerId, ct);

                // Lấy VIP level hiện tại
                var currentLevel = await _db.VIPLevels
                    .FirstOrDefaultAsync(l => l.VipLevelId == vipMember.CurrentVipLevelId, ct);

                if (currentLevel == null)
                {
                    _logger.LogError("VIP Level {LevelId} không tồn tại", vipMember.CurrentVipLevelId);
                    throw new NotFoundException("VIP Level không tồn tại");
                }

                // Tính điểm: 1 VND = 1 điểm, nhân với tỷ lệ tích điểm của VIP level
                var basePoints = (int)Math.Floor(orderAmount); // Làm tròn xuống
                var earnedPoints = (int)Math.Floor(basePoints * currentLevel.PointEarningRate);

                // Cập nhật điểm
                vipMember.TotalPoints += earnedPoints;
                vipMember.GrowthValue += earnedPoints; // Growth value cũng tăng
                vipMember.UpdatedAt = DateTime.UtcNow;

                // Lưu lịch sử tích điểm
                var pointHistory = new PointHistory
                {
                    CustomerId = customerId,
                    OrderId = orderId,
                    TransactionType = "EARNED",
                    Points = earnedPoints,
                    Description = $"Tích điểm từ đơn hàng {orderId}. Số tiền: {orderAmount:N0} VND",
                    VipLevelId = currentLevel.VipLevelId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.PointHistories.Add(pointHistory);

                // Đồng bộ với Customer.LoyaltyPoints (để tương thích ngược)
                var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
                if (customer != null)
                {
                    customer.LoyaltyPoints = vipMember.TotalPoints;
                }

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Đã tích {Points} điểm cho Customer {CustomerId} từ Order {OrderId}. Tổng điểm: {TotalPoints}",
                    earnedPoints, customerId, orderId, vipMember.TotalPoints);

                // Kiểm tra và nâng cấp VIP level
                await CheckAndUpgradeVIPLevelAsync(customerId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tích điểm cho Order {OrderId}, Customer {CustomerId}", orderId, customerId);
                throw;
            }
        }

        public async Task<VIPStatusResponse> GetVIPStatusAsync(int customerId, CancellationToken ct = default)
        {
            var vipMember = await GetOrCreateVIPMemberAsync(customerId, ct);

            var currentLevel = await _db.VIPLevels
                .Include(l => l.VIPBenefits.Where(b => b.IsActive))
                .FirstOrDefaultAsync(l => l.VipLevelId == vipMember.CurrentVipLevelId, ct);

            if (currentLevel == null)
            {
                throw new NotFoundException("VIP Level không tồn tại");
            }

            // Lấy VIP level tiếp theo
            var nextLevel = await _db.VIPLevels
                .Where(l => l.VipLevelId > vipMember.CurrentVipLevelId && l.IsActive)
                .OrderBy(l => l.VipLevelId)
                .FirstOrDefaultAsync(ct);

            // Tính phần trăm progress
            int progressPercent = 0;
            int pointsNeeded = 0;
            if (nextLevel != null)
            {
                pointsNeeded = nextLevel.MinPointsRequired - vipMember.GrowthValue;
                var totalNeeded = nextLevel.MinPointsRequired - currentLevel.MinPointsRequired;
                if (totalNeeded > 0)
                {
                    var currentProgress = vipMember.GrowthValue - currentLevel.MinPointsRequired;
                    progressPercent = (int)Math.Min(100, Math.Max(0, (currentProgress * 100) / totalNeeded));
                }
            }
            else
            {
                // Đã đạt VIP level cao nhất
                progressPercent = 100;
            }

            // Lấy các quyền lợi đã kích hoạt và chưa kích hoạt
            var activatedBenefits = currentLevel.VIPBenefits
                .Where(b => b.IsActive)
                .Select(b => new VIPBenefitInfo
                {
                    BenefitId = b.BenefitId,
                    BenefitType = b.BenefitType,
                    BenefitName = b.BenefitName,
                    BenefitDescription = b.BenefitDescription,
                    BenefitValue = b.BenefitValue,
                    IsActivated = IsBenefitActivated(b, vipMember)
                })
                .ToList();

            // Lấy tất cả VIP levels để hiển thị progression bar
            var allLevels = await _db.VIPLevels
                .Where(l => l.IsActive)
                .OrderBy(l => l.VipLevelId)
                .Select(l => new VIPLevelInfo
                {
                    VipLevelId = l.VipLevelId,
                    LevelName = l.LevelName,
                    LevelDisplayName = l.LevelDisplayName,
                    MinPointsRequired = l.MinPointsRequired,
                    IsCurrentLevel = l.VipLevelId == vipMember.CurrentVipLevelId
                })
                .ToListAsync(ct);

            return new VIPStatusResponse
            {
                CurrentVipLevelId = currentLevel.VipLevelId,
                LevelName = currentLevel.LevelName,
                LevelDisplayName = currentLevel.LevelDisplayName,
                TotalPoints = vipMember.TotalPoints,
                GrowthValue = vipMember.GrowthValue,
                ProgressPercent = progressPercent,
                PointsNeeded = pointsNeeded,
                NextLevelName = nextLevel?.LevelName,
                NextLevelDisplayName = nextLevel?.LevelDisplayName,
                ActivatedBenefits = activatedBenefits,
                AllLevels = allLevels,
                LastUpgradeDate = vipMember.LastUpgradeDate
            };
        }

        public async Task<PointHistoryResponse> GetPointHistoryAsync(int customerId, int page = 1, int limit = 20, CancellationToken ct = default)
        {
            var query = _db.PointHistories
                .Where(ph => ph.CustomerId == customerId)
                .Include(ph => ph.VIPLevel)
                .OrderByDescending(ph => ph.CreatedAt);

            var total = await query.CountAsync(ct);

            var histories = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(ph => new PointHistoryItem
                {
                    PointHistoryId = ph.PointHistoryId,
                    OrderId = ph.OrderId,
                    TransactionType = ph.TransactionType,
                    Points = ph.Points,
                    Description = ph.Description,
                    VipLevelName = ph.VIPLevel != null ? ph.VIPLevel.LevelName : null,
                    CreatedAt = ph.CreatedAt
                })
                .ToListAsync(ct);

            return new PointHistoryResponse
            {
                Items = histories,
                Total = total,
                Page = page,
                Limit = limit,
                TotalPages = (int)Math.Ceiling(total / (double)limit)
            };
        }

        public async Task<bool> CheckAndUpgradeVIPLevelAsync(int customerId, CancellationToken ct = default)
        {
            var vipMember = await GetOrCreateVIPMemberAsync(customerId, ct);

            // Lấy VIP level hiện tại
            var currentLevel = await _db.VIPLevels
                .FirstOrDefaultAsync(l => l.VipLevelId == vipMember.CurrentVipLevelId, ct);

            if (currentLevel == null)
            {
                return false;
            }

            // Tìm VIP level cao hơn mà customer đủ điều kiện
            var eligibleLevel = await _db.VIPLevels
                .Where(l => l.VipLevelId > vipMember.CurrentVipLevelId
                    && l.IsActive
                    && vipMember.GrowthValue >= l.MinPointsRequired)
                .OrderByDescending(l => l.VipLevelId)
                .FirstOrDefaultAsync(ct);

            if (eligibleLevel == null)
            {
                return false;
            }

            // Nâng cấp VIP level
            var oldLevelId = vipMember.CurrentVipLevelId;
            vipMember.CurrentVipLevelId = eligibleLevel.VipLevelId;
            vipMember.LastUpgradeDate = DateTime.UtcNow;
            vipMember.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Customer {CustomerId} đã nâng cấp từ VIP{OldLevel} lên VIP{NewLevel}",
                customerId, oldLevelId, eligibleLevel.VipLevelId);

            // Tự động tạo UserVoucher cho quà nâng cấp VIP
            var upgradeBenefit = await _db.VIPBenefits
                .FirstOrDefaultAsync(b => b.VipLevelId == eligibleLevel.VipLevelId
                    && b.BenefitType == "UPGRADE_BONUS"
                    && b.IsActive, ct);

            if (upgradeBenefit != null)
            {
                // Tìm voucher cố định theo mã VIP level (VIP1, VIP2, VIP3, VIP4)
                var voucherCode = eligibleLevel.LevelName; // "VIP1", "VIP2", "VIP3", "VIP4"
                var voucher = await _db.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode && v.IsActive, ct);

                if (voucher != null)
                {
                    // Lấy UserId từ Customer
                    var customer = await _db.Customers
                        .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

                    if (customer != null)
                    {
                        // Kiểm tra xem user đã có UserVoucher cho voucher này chưa
                        var existingUserVoucher = await _db.UserVouchers
                            .FirstOrDefaultAsync(uv => uv.UserId == customer.UserId 
                                && uv.VoucherId == voucher.VoucherId, ct);

                        if (existingUserVoucher == null)
                        {
                            // Tạo UserVoucher mới - user có thể sử dụng voucher này
                            var userVoucher = new UserVoucher
                            {
                                UserId = customer.UserId,
                                VoucherId = voucher.VoucherId,
                                IsUsed = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.UserVouchers.Add(userVoucher);

                            _logger.LogInformation(
                                "Đã tự động tạo UserVoucher cho Customer {CustomerId} với voucher {VoucherCode} khi nâng cấp lên {LevelName}",
                                customerId, voucherCode, eligibleLevel.LevelName);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Customer {CustomerId} đã có UserVoucher cho voucher {VoucherCode} rồi",
                                customerId, voucherCode);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Không tìm thấy voucher với mã {VoucherCode} cho VIP level {LevelId}",
                        voucherCode, eligibleLevel.VipLevelId);
                }

                // Tạo benefit claim record để tracking
                var claim = new VIPBenefitClaim
                {
                    VipMemberId = vipMember.VipMemberId,
                    BenefitId = upgradeBenefit.BenefitId,
                    ClaimType = "UPGRADE_BONUS",
                    ClaimValue = upgradeBenefit.BenefitValue,
                    Status = "CLAIMED", // Đã tự động claim
                    ClaimedAt = DateTime.UtcNow,
                    VoucherId = voucher?.VoucherId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.VIPBenefitClaims.Add(claim);
            }

            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<ClaimBenefitResponse> ClaimUpgradeBonusAsync(int customerId, CancellationToken ct = default)
        {
            var vipMember = await _db.VIPMembers
                .Include(vm => vm.Customer)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(vm => vm.CustomerId == customerId, ct);

            if (vipMember == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin VIP");
            }

            // Lấy VIP level hiện tại
            var currentLevel = await _db.VIPLevels
                .FirstOrDefaultAsync(l => l.VipLevelId == vipMember.CurrentVipLevelId, ct);

            if (currentLevel == null)
            {
                throw new NotFoundException("Không tìm thấy VIP level");
            }

            // Tìm voucher cố định theo mã VIP level
            var voucherCode = currentLevel.LevelName; // "VIP1", "VIP2", "VIP3", "VIP4"
            var voucher = await _db.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode && v.IsActive, ct);

            if (voucher == null)
            {
                throw new NotFoundException($"Không tìm thấy voucher {voucherCode}");
            }

            // Kiểm tra xem user đã có UserVoucher chưa
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (customer == null)
            {
                throw new NotFoundException("Không tìm thấy customer");
            }

            var existingUserVoucher = await _db.UserVouchers
                .FirstOrDefaultAsync(uv => uv.UserId == customer.UserId 
                    && uv.VoucherId == voucher.VoucherId, ct);

            if (existingUserVoucher != null)
            {
                // User đã có voucher này rồi
                if (existingUserVoucher.IsUsed)
                {
                    throw new ValidationException("voucher", "Bạn đã sử dụng voucher này rồi");
                }

                return new ClaimBenefitResponse
                {
                    Success = true,
                    Message = $"Bạn đã có voucher {voucherCode} trong tài khoản. Vui lòng sử dụng khi đặt vé.",
                    VoucherId = voucher.VoucherId,
                    ClaimValue = voucher.DiscountVal
                };
            }

            // Tạo UserVoucher mới
            var userVoucher = new UserVoucher
            {
                UserId = customer.UserId,
                VoucherId = voucher.VoucherId,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.UserVouchers.Add(userVoucher);

            // Tìm hoặc tạo benefit claim record
            var upgradeBenefit = await _db.VIPBenefits
                .FirstOrDefaultAsync(b => b.VipLevelId == currentLevel.VipLevelId
                    && b.BenefitType == "UPGRADE_BONUS"
                    && b.IsActive, ct);

            if (upgradeBenefit != null)
            {
                var claim = await _db.VIPBenefitClaims
                    .FirstOrDefaultAsync(c => c.VipMemberId == vipMember.VipMemberId
                        && c.BenefitId == upgradeBenefit.BenefitId
                        && c.ClaimType == "UPGRADE_BONUS", ct);

                if (claim == null)
                {
                    claim = new VIPBenefitClaim
                    {
                        VipMemberId = vipMember.VipMemberId,
                        BenefitId = upgradeBenefit.BenefitId,
                        ClaimType = "UPGRADE_BONUS",
                        ClaimValue = upgradeBenefit.BenefitValue,
                        Status = "CLAIMED",
                        ClaimedAt = DateTime.UtcNow,
                        VoucherId = voucher.VoucherId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.VIPBenefitClaims.Add(claim);
                }
                else
                {
                    claim.Status = "CLAIMED";
                    claim.ClaimedAt = DateTime.UtcNow;
                    claim.VoucherId = voucher.VoucherId;
                }
            }

            await _db.SaveChangesAsync(ct);

            return new ClaimBenefitResponse
            {
                Success = true,
                Message = $"Đã nhận voucher {voucherCode} thành công. Vui lòng sử dụng khi đặt vé.",
                VoucherId = voucher.VoucherId,
                ClaimValue = voucher.DiscountVal
            };
        }

        public async Task<ClaimBenefitResponse> ClaimBirthdayBonusAsync(int customerId, CancellationToken ct = default)
        {
            var customer = await _db.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (customer == null)
            {
                throw new NotFoundException("Không tìm thấy customer");
            }

            if (!customer.DateOfBirth.HasValue)
            {
                throw new ValidationException("DateOfBirth", "Chưa cập nhật ngày sinh nhật");
            }

            var vipMember = await GetOrCreateVIPMemberAsync(customerId, ct);
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var currentDay = DateTime.UtcNow.Day;

            // Kiểm tra xem đã nhận quà sinh nhật năm nay chưa
            if (vipMember.BirthdayBonusClaimedYear == currentYear)
            {
                throw new ValidationException("birthday", "Đã nhận quà sinh nhật năm nay rồi");
            }

            // Kiểm tra xem có phải sinh nhật không
            if (customer.DateOfBirth.Value.Month != currentMonth || customer.DateOfBirth.Value.Day != currentDay)
            {
                throw new ValidationException("birthday", "Chưa đến ngày sinh nhật");
            }

            // Lấy birthday bonus benefit của VIP level hiện tại
            var birthdayBenefit = await _db.VIPBenefits
                .FirstOrDefaultAsync(b => b.VipLevelId == vipMember.CurrentVipLevelId
                    && b.BenefitType == "BIRTHDAY_BONUS"
                    && b.IsActive, ct);

            if (birthdayBenefit == null)
            {
                throw new NotFoundException("Không có quà sinh nhật cho VIP level này");
            }

            // Tạo voucher
            int? voucherId = null;
            if (birthdayBenefit.BenefitValue.HasValue && birthdayBenefit.BenefitValue.Value > 0)
            {
                var managers = await _db.Managers.Take(1).ToListAsync(ct);
                if (managers.Count > 0)
                {
                    var managerId = managers[0].ManagerId;
                    var voucherCode = $"BIRTHDAY_{customer.UserId}_{DateTime.UtcNow:yyyyMMdd}";

                    var currentLevel = await _db.VIPLevels
                        .FirstOrDefaultAsync(l => l.VipLevelId == vipMember.CurrentVipLevelId, ct);
                    var levelDisplayName = currentLevel?.LevelDisplayName ?? "VIP";

                    var voucher = new Voucher
                    {
                        VoucherCode = voucherCode,
                        DiscountType = "fixed",
                        DiscountVal = birthdayBenefit.BenefitValue.Value,
                        ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                        ValidTo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                        ManagerId = managerId,
                        Description = $"Quà sinh nhật VIP {levelDisplayName}",
                        IsActive = true,
                        IsRestricted = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Vouchers.Add(voucher);
                    await _db.SaveChangesAsync(ct);
                    voucherId = voucher.VoucherId;

                    var userVoucher = new UserVoucher
                    {
                        UserId = customer.UserId,
                        VoucherId = voucher.VoucherId,
                        IsUsed = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.UserVouchers.Add(userVoucher);
                }
            }

            // Cập nhật năm đã nhận
            vipMember.BirthdayBonusClaimedYear = currentYear;
            vipMember.UpdatedAt = DateTime.UtcNow;

            // Tạo claim record
            var claim = new VIPBenefitClaim
            {
                VipMemberId = vipMember.VipMemberId,
                BenefitId = birthdayBenefit.BenefitId,
                ClaimType = "BIRTHDAY_BONUS",
                ClaimValue = birthdayBenefit.BenefitValue,
                Status = "CLAIMED",
                ClaimedAt = DateTime.UtcNow,
                VoucherId = voucherId,
                CreatedAt = DateTime.UtcNow
            };

            _db.VIPBenefitClaims.Add(claim);
            await _db.SaveChangesAsync(ct);

            return new ClaimBenefitResponse
            {
                Success = true,
                Message = "Đã nhận quà sinh nhật thành công",
                VoucherId = voucherId,
                ClaimValue = birthdayBenefit.BenefitValue
            };
        }

        private async Task<VIPMember> GetOrCreateVIPMemberAsync(int customerId, CancellationToken ct = default)
        {
            var vipMember = await _db.VIPMembers
                .Include(vm => vm.CurrentVIPLevel)
                .FirstOrDefaultAsync(vm => vm.CustomerId == customerId, ct);

            if (vipMember == null)
            {
                // Tạo VIPMember mới với VIP0
                vipMember = new VIPMember
                {
                    CustomerId = customerId,
                    CurrentVipLevelId = 0, // VIP0
                    TotalPoints = 0,
                    GrowthValue = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _db.VIPMembers.Add(vipMember);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Đã tạo VIPMember mới cho Customer {CustomerId}", customerId);
            }

            return vipMember;
        }

        private bool IsBenefitActivated(VIPBenefit benefit, VIPMember vipMember)
        {
            return benefit.BenefitType switch
            {
                "UPGRADE_BONUS" => vipMember.LastUpgradeDate.HasValue,
                "BIRTHDAY_BONUS" => vipMember.BirthdayBonusClaimedYear.HasValue,
                "DISCOUNT_VOUCHER" => true, // Voucher luôn available
                "FREE_TICKET" => vipMember.MonthlyFreeTicketClaimedMonth.HasValue,
                "FREE_COMBO" => vipMember.MonthlyFreeComboClaimedMonth.HasValue,
                "PRIORITY_BOOKING" => true, // Priority booking luôn active
                _ => false
            };
        }
    }
}

