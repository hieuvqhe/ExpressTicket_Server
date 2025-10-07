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

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual Employee? Employee { get; set; }

    public virtual Manager? Manager { get; set; }

    public virtual Partner? Partner { get; set; }

    public virtual ICollection<RatingFilm> RatingFilms { get; set; } = new List<RatingFilm>();

    public virtual SystemAdmin? SystemAdmin { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();

}
