namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;

/// <summary>
/// Response cho Permission
/// </summary>
public class PermissionResponse
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Response cho Employee Permission
/// </summary>
public class EmployeePermissionResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int? CinemaId { get; set; }
    public string? CinemaName { get; set; }
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public DateTime GrantedAt { get; set; }
    public string GrantedByName { get; set; } = null!;
    public bool IsActive { get; set; }
}

/// <summary>
/// Response danh sách permissions của một employee (grouped by cinema)
/// </summary>
public class EmployeePermissionsListResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public List<CinemaPermissionsGroup> CinemaPermissions { get; set; } = new();
}

/// <summary>
/// Permissions được nhóm theo từng Cinema
/// </summary>
public class CinemaPermissionsGroup
{
    public int CinemaId { get; set; }
    public string CinemaName { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public List<PermissionDetailResponse> Permissions { get; set; } = new();
}

/// <summary>
/// Chi tiết từng permission
/// </summary>
public class PermissionDetailResponse
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime GrantedAt { get; set; }
    public int GrantedByUserId { get; set; }
    public string GrantedByName { get; set; } = null!;
    public string? GrantedByEmail { get; set; }
    public bool IsActive { get; set; }
    public bool IsGlobalPermission { get; set; } // True nếu được cấp từ global permission
}

/// <summary>
/// Response cho action grant/revoke permission
/// </summary>
public class PermissionActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int AffectedCount { get; set; }
}

/// <summary>
/// Response danh sách tất cả permissions trong hệ thống
/// </summary>
public class AvailablePermissionsResponse
{
    public List<PermissionGroupResponse> PermissionGroups { get; set; } = new();
}

/// <summary>
/// Permission được nhóm theo Resource Type
/// </summary>
public class PermissionGroupResponse
{
    public string ResourceType { get; set; } = null!;
    public string ResourceName { get; set; } = null!;
    public List<PermissionResponse> Permissions { get; set; } = new();
}


