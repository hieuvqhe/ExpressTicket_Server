using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class ManagerStaffPartnerPermission
{
    public int Id { get; set; }

    public int ManagerStaffId { get; set; }

    public int? PartnerId { get; set; }  // NULL = áp dụng cho tất cả partners được assign

    public int PermissionId { get; set; }

    public int GrantedBy { get; set; }  // User ID của Manager cấp quyền

    public DateTime GrantedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedBy { get; set; }

    public virtual ManagerStaff ManagerStaff { get; set; } = null!;

    public virtual Partner? Partner { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User GrantedByUser { get; set; } = null!;

    public virtual User? RevokedByUser { get; set; }
}








