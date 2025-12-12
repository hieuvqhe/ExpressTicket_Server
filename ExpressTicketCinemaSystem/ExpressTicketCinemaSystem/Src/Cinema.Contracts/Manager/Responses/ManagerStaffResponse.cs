namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class ManagerStaffResponse
    {
        public int ManagerStaffId { get; set; }
        public int UserId { get; set; }
        public int ManagerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; }
    }
}








