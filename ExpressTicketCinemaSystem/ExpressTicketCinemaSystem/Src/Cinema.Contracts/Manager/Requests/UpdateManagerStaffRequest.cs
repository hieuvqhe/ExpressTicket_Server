namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class UpdateManagerStaffRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? RoleType { get; set; } // "ManagerStaff"
        public bool? IsActive { get; set; }
    }
}








