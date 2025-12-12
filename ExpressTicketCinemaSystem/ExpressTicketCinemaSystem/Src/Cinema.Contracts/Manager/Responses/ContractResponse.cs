namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class ContractResponse
    {
        // Contract basic information
        public int ContractId { get; set; }
        public int ManagerId { get; set; }
        public int PartnerId { get; set; }
        public int? CreatedBy { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TermsAndConditions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal? MinimumRevenue { get; set; }

        // Status and security
        public string Status { get; set; } = "Draft";
        public bool IsLocked { get; set; }
        public string ContractHash { get; set; } = string.Empty;

        // Contract PDF URL (original PDF from manager, replaced by signed PDF from partner)
        public string? PdfUrl { get; set; }

        // Signature information
        public string? PartnerSignatureUrl { get; set; }
        public string? ManagerSignature { get; set; }
        public DateTime? SignedAt { get; set; }
        public DateTime? PartnerSignedAt { get; set; }
        public DateTime? ManagerSignedAt { get; set; }
        public int? ManagerStaffId { get; set; }
        public string? ManagerStaffSignature { get; set; }
        public DateTime? ManagerStaffSignedAt { get; set; }
        public DateTime? LockedAt { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Partner information (for PDF - Party A)
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public string PartnerTaxCode { get; set; } = string.Empty;
        public string PartnerRepresentative { get; set; } = string.Empty;
        public string PartnerPosition { get; set; } = string.Empty;
        public string PartnerEmail { get; set; } = string.Empty;
        public string PartnerPhone { get; set; } = string.Empty;

        // Company information (for PDF - Party B)
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyTaxCode { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerPosition { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;

        // ManagerStaff information (if contract was created/signed by ManagerStaff)
        public string? ManagerStaffName { get; set; }
        public string? ManagerStaffEmail { get; set; }
        public bool HasManagerStaffSignedTemporarily { get; set; }
    }
}