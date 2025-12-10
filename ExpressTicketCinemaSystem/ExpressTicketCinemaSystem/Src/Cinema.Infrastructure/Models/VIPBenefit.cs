using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VIPBenefit
{
    public int BenefitId { get; set; }

    public int VipLevelId { get; set; }

    public string BenefitType { get; set; } = null!; // UPGRADE_BONUS, BIRTHDAY_BONUS, DISCOUNT_VOUCHER, FREE_TICKET, PRIORITY_BOOKING, FREE_COMBO

    public string BenefitName { get; set; } = null!;

    public string? BenefitDescription { get; set; }

    public decimal? BenefitValue { get; set; } // Giá trị quyền lợi (ví dụ: 50000 cho upgrade bonus)

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual VIPLevel VIPLevel { get; set; } = null!;

    public virtual ICollection<VIPBenefitClaim> VIPBenefitClaims { get; set; } = new List<VIPBenefitClaim>();
}










