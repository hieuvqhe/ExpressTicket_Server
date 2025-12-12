using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class VIPLevel
{
    public int VipLevelId { get; set; }

    public string LevelName { get; set; } = null!; // VIP0, VIP1, VIP2, VIP3, VIP4

    public string LevelDisplayName { get; set; } = null!; // "Thành viên", "Đồng", "Bạc", "Vàng", "Kim Cương"

    public int MinPointsRequired { get; set; } // Điểm tối thiểu để đạt cấp độ này

    public decimal PointEarningRate { get; set; } = 1.00m; // Tỷ lệ tích điểm (1.00 = 1:1, 1.50 = 1.5x)

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<VIPBenefit> VIPBenefits { get; set; } = new List<VIPBenefit>();

    public virtual ICollection<VIPMember> VIPMembers { get; set; } = new List<VIPMember>();

    public virtual ICollection<PointHistory> PointHistories { get; set; } = new List<PointHistory>();
}


















