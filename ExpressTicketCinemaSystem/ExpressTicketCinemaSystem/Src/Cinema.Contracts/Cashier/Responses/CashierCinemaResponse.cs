namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Responses;

/// <summary>
/// Response DTO for cashier's assigned cinema information
/// </summary>
public class CashierCinemaResponse
{
    public int CinemaId { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Code { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime AssignedAt { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
}









