using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request
{
    public class AdminUpdateUserRequest
    {
        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? UserType { get; set; }

        public string? Fullname { get; set; }

        public bool? IsActive { get; set; }

        public bool? EmailConfirmed { get; set; }

        public string? Username { get; set; }

        public string? AvataUrl { get; set; }

        public bool? IsBanned { get; set; }
    }
}