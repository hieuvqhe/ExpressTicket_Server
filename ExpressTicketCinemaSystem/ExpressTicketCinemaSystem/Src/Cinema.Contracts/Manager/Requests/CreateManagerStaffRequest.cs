namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class CreateManagerStaffRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string RoleType { get; set; } = "ManagerStaff"; // Default to ManagerStaff
        public DateOnly HireDate { get; set; }
    }
}








