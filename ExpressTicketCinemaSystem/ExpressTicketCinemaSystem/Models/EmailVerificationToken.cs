using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

public partial class EmailVerificationToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public byte[] TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
