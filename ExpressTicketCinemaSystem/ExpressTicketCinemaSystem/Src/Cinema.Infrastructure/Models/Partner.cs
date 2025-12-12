using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Partner
{
    public int PartnerId { get; set; }

    public int UserId { get; set; }

    public int? ManagerId { get; set; }

    public int? ManagerStaffId { get; set; }

    public string PartnerName { get; set; } = null!;

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal? CommissionRate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? BusinessRegistrationCertificateUrl { get; set; }

    public string? TaxRegistrationCertificateUrl { get; set; }

    public string? IdentityCardUrl { get; set; }

    public string? TheaterPhotosUrl { get; set; }

    public string? AdditionalDocumentsUrl { get; set; }

    public string? Status { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();

    public virtual ICollection<Cinema> Cinemas { get; set; } = new List<Cinema>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<GameShow> GameShows { get; set; } = new List<GameShow>();

    public virtual Manager? Manager { get; set; }

    public virtual ManagerStaff? ManagerStaff { get; set; }

    public virtual ICollection<MovieSubmission> MovieSubmissions { get; set; } = new List<MovieSubmission>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual ICollection<PartnerReport> PartnerReports { get; set; } = new List<PartnerReport>();

    public virtual ICollection<SeatType> SeatTypes { get; set; } = new List<SeatType>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual User User { get; set; } = null!;
}
