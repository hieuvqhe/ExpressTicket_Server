using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class EmailChangeRequest
{
    public Guid RequestId { get; set; }

    public int UserId { get; set; }

    public string? NewEmail { get; set; }

    public byte[]? CurrentCodeHash { get; set; }

    public byte[]? NewCodeHash { get; set; }

    public DateTime? CurrentCodeExpiresAt { get; set; }

    public DateTime? NewCodeExpiresAt { get; set; }

    public bool CurrentVerified { get; set; }

    public bool NewVerified { get; set; }

    public bool IsConsumed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
