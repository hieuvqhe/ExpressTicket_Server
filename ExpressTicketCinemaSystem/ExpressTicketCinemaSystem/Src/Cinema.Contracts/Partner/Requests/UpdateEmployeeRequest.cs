namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UpdateEmployeeRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? RoleType { get; set; } // "Staff", "Marketing", "Cashier"
        public bool? IsActive { get; set; }
    }
}




















