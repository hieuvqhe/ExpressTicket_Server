using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VoucherEmailHistory
{
    [Key]
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int UserId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "success";

    // Navigation properties
    public virtual Voucher Voucher { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}