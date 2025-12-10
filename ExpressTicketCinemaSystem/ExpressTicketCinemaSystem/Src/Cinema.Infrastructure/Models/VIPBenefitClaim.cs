using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VIPBenefitClaim
{
    public long BenefitClaimId { get; set; }

    public int VipMemberId { get; set; }

    public int BenefitId { get; set; }

    public string ClaimType { get; set; } = null!; // UPGRADE_BONUS, BIRTHDAY_BONUS, MONTHLY_FREE_TICKET, MONTHLY_FREE_COMBO

    public decimal? ClaimValue { get; set; }

    public int? VoucherId { get; set; } // Nếu quyền lợi là voucher, lưu voucher_id

    public string Status { get; set; } = "PENDING"; // PENDING, CLAIMED, EXPIRED

    public DateTime? ClaimedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual VIPMember VIPMember { get; set; } = null!;

    public virtual VIPBenefit VIPBenefit { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}










