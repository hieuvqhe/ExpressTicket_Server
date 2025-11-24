using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int PartnerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string RoleType { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Partner Partner { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<EmployeeCinemaAssignment> CinemaAssignments { get; set; } = new List<EmployeeCinemaAssignment>();
}
