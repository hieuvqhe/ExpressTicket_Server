using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class ManagerStaff
{
    public int ManagerStaffId { get; set; }

    public int ManagerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string RoleType { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Manager Manager { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Partner> Partners { get; set; } = new List<Partner>();

    public virtual ICollection<ManagerStaffPartnerPermission> PartnerPermissions { get; set; } = new List<ManagerStaffPartnerPermission>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

