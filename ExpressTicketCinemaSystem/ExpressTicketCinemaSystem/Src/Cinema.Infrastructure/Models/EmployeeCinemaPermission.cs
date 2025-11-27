using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class EmployeeCinemaPermission
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int? CinemaId { get; set; }  // NULL = áp dụng cho tất cả cinemas

    public int PermissionId { get; set; }

    public int GrantedBy { get; set; }  // User ID của Partner cấp quyền

    public DateTime GrantedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? RevokedBy { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Cinema? Cinema { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User GrantedByUser { get; set; } = null!;

    public virtual User? RevokedByUser { get; set; }
}




