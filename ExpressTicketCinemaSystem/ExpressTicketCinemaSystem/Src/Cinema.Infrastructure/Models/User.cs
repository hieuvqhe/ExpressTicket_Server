using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Phone { get; set; }

    public string UserType { get; set; } = null!;

    public string? Fullname { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool EmailConfirmed { get; set; }

    public string Username { get; set; } = null!;

    public string AvatarUrl { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public bool IsBanned { get; set; }

    public DateTime? BannedAt { get; set; }

    public DateTime? UnbannedAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<EmailChangeRequest> EmailChangeRequests { get; set; } = new List<EmailChangeRequest>();

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual Employee? Employee { get; set; }

    public virtual Manager? Manager { get; set; }

    public virtual ManagerStaff? ManagerStaff { get; set; }

    public virtual Partner? Partner { get; set; }

    public virtual ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();

    public virtual ICollection<RatingFilm> RatingFilms { get; set; } = new List<RatingFilm>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual SystemAdmin? SystemAdmin { get; set; }

    public virtual ICollection<VoucherEmailHistory> VoucherEmailHistories { get; set; } = new List<VoucherEmailHistory>();
}
