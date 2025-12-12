using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    /// <summary>
    /// Response chứa thông tin profile của ManagerStaff
    /// </summary>
    public class ManagerStaffProfileResponse
    {
        public int ManagerStaffId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string RoleType { get; set; } = null!;
        public bool IsActive { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = null!;
        public string? ManagerEmail { get; set; }
        public DateTime HireDate { get; set; }
        public List<AssignedPartnerInfo> AssignedPartners { get; set; } = new();
        public List<GrantedPermissionInfo> GrantedPermissions { get; set; } = new();
    }

    /// <summary>
    /// Thông tin Partner được phân công cho ManagerStaff
    /// </summary>
    public class AssignedPartnerInfo
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = null!;
        public DateTime AssignedAt { get; set; }
        public int? AssignedByUserId { get; set; }
        public string? AssignedByEmail { get; set; }
        public string? AssignedByName { get; set; }
    }

    /// <summary>
    /// Thông tin quyền được cấp cho ManagerStaff
    /// </summary>
    public class GrantedPermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = null!;
        public string PermissionName { get; set; } = null!;
        public string ResourceType { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string? Description { get; set; }
        public int? PartnerId { get; set; } // NULL = global permission cho tất cả partners được phân công
        public string? PartnerName { get; set; } // NULL nếu là global permission
        public DateTime GrantedAt { get; set; }
        public int GrantedByUserId { get; set; }
        public string GrantedByEmail { get; set; } = null!;
        public string? GrantedByName { get; set; }
        public bool IsActive { get; set; }
    }
}








