using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Contract
{
    public int ContractId { get; set; }

    public int ManagerId { get; set; }

    public int PartnerId { get; set; }

    public int? CreatedBy { get; set; }

    public string ContractNumber { get; set; } = null!;

    public string ContractType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? TermsAndConditions { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal CommissionRate { get; set; }

    public decimal? MinimumRevenue { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public string? ContractHash { get; set; }

    public string? PartnerSignatureUrl { get; set; }

    public string? ManagerSignature { get; set; }

    public DateTime? SignedAt { get; set; }

    public DateTime? PartnerSignedAt { get; set; }

    public DateTime? ManagerSignedAt { get; set; }

    public int? ManagerStaffId { get; set; }

    public string? ManagerStaffSignature { get; set; }

    public DateTime? ManagerStaffSignedAt { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string? PdfUrl { get; set; }

    public virtual Manager Manager { get; set; } = null!;

    public virtual Partner Partner { get; set; } = null!;

    public virtual ManagerStaff? ManagerStaff { get; set; }
}
