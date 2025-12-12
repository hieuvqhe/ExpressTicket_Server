using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class AssignCinemaToEmployeeRequest
    {
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất một rạp được chọn")]
        public List<int> CinemaIds { get; set; } = new List<int>();
    }
}








