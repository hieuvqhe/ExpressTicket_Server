using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Models;

public partial class Contract
{
    public int ContractId { get; set; }

    public int ManagerId { get; set; }

    public int PartnerId { get; set; }

    public string ContractNumber { get; set; } = null!;

    public string? ContractType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal? CommissionRate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SignedAt { get; set; }

    public virtual Manager Manager { get; set; } = null!;

    public virtual Partner Partner { get; set; } = null!;
}
