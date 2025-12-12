using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VIPMember
{
    public int VipMemberId { get; set; }

    public int CustomerId { get; set; }

    public int CurrentVipLevelId { get; set; } = 0;

    public int TotalPoints { get; set; } = 0; // Tổng điểm tích lũy (không bao gồm điểm đã dùng)

    public int GrowthValue { get; set; } = 0; // Giá trị tăng trưởng (điểm tích lũy trong kỳ hiện tại để nâng cấp)

    public DateTime? LastUpgradeDate { get; set; }

    public int? BirthdayBonusClaimedYear { get; set; } // Năm đã nhận quà sinh nhật gần nhất

    public int? MonthlyFreeTicketClaimedMonth { get; set; } // Tháng đã nhận vé miễn phí gần nhất (format: YYYYMM)

    public int? MonthlyFreeComboClaimedMonth { get; set; } // Tháng đã nhận combo miễn phí gần nhất (format: YYYYMM)

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual VIPLevel CurrentVIPLevel { get; set; } = null!;

    public virtual ICollection<VIPBenefitClaim> VIPBenefitClaims { get; set; } = new List<VIPBenefitClaim>();
}


















