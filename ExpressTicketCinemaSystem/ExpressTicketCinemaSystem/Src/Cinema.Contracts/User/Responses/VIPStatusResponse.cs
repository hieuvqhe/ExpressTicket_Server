namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;

public class VIPStatusResponse
{
    public int CurrentVipLevelId { get; set; }
    public string LevelName { get; set; } = null!;
    public string LevelDisplayName { get; set; } = null!;
    public int TotalPoints { get; set; }
    public int GrowthValue { get; set; }
    public int ProgressPercent { get; set; }
    public int PointsNeeded { get; set; }
    public string? NextLevelName { get; set; }
    public string? NextLevelDisplayName { get; set; }
    public List<VIPBenefitInfo> ActivatedBenefits { get; set; } = new();
    public List<VIPLevelInfo> AllLevels { get; set; } = new();
    public DateTime? LastUpgradeDate { get; set; }
}

public class VIPBenefitInfo
{
    public int BenefitId { get; set; }
    public string BenefitType { get; set; } = null!;
    public string BenefitName { get; set; } = null!;
    public string? BenefitDescription { get; set; }
    public decimal? BenefitValue { get; set; }
    public bool IsActivated { get; set; }
}

public class VIPLevelInfo
{
    public int VipLevelId { get; set; }
    public string LevelName { get; set; } = null!;
    public string LevelDisplayName { get; set; } = null!;
    public int MinPointsRequired { get; set; }
    public bool IsCurrentLevel { get; set; }
}


















