namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class EmployeeCinemaAssignmentResponse
    {
        public int AssignmentId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string? CinemaCity { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
    }
}












