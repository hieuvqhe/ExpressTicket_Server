namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class EmployeeResponse
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; }
    }
}










































