namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateEmployeeRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty; // "Staff", "Marketing", "Cashier"
        public DateOnly HireDate { get; set; }
    }
}












