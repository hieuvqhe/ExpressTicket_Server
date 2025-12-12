using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

/// <summary>
/// Response chứa danh sách permissions của ManagerStaff, nhóm theo Partner
/// </summary>
public class ManagerStaffPermissionsListResponse
{
    public int ManagerStaffId { get; set; }
    public string ManagerStaffName { get; set; } = string.Empty;
    public List<PartnerPermissionsGroup> PartnerPermissions { get; set; } = new();
}

/// <summary>
/// Nhóm permissions theo Partner
/// </summary>
public class PartnerPermissionsGroup
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public List<PermissionDetailResponse> Permissions { get; set; } = new();
}








