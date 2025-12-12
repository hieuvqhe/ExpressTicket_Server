using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class EmployeeCinemaAssignment
{
    public int AssignmentId { get; set; }

    public int EmployeeId { get; set; }

    public int CinemaId { get; set; }

    public DateTime AssignedAt { get; set; }

    public int? AssignedBy { get; set; } // UserId của Partner gán quyền

    public bool IsActive { get; set; }

    public DateTime? UnassignedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual User? AssignedByUser { get; set; }
}














