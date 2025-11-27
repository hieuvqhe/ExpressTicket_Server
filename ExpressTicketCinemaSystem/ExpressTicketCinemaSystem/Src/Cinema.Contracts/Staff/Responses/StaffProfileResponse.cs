using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Staff.Responses
{
    public class StaffProfileResponse
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string RoleType { get; set; } = null!;
        public bool IsActive { get; set; }
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = null!;
        public List<AssignedCinemaInfo> AssignedCinemas { get; set; } = new();
        public List<GrantedPermissionInfo> GrantedPermissions { get; set; } = new();
    }

    public class AssignedCinemaInfo
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public DateTime AssignedAt { get; set; }
        public int? AssignedByUserId { get; set; }
        public string? AssignedByEmail { get; set; }
        public string? AssignedByName { get; set; }
    }

    public class GrantedPermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = null!;
        public string PermissionName { get; set; } = null!;
        public string ResourceType { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string? Description { get; set; }
        public int CinemaId { get; set; } // ID của cinema cụ thể
        public string CinemaName { get; set; } = null!; // Tên cinema cụ thể
        public DateTime GrantedAt { get; set; }
        public int GrantedByUserId { get; set; }
        public string GrantedByEmail { get; set; } = null!;
        public string? GrantedByName { get; set; }
        public bool IsActive { get; set; }
    }
}


