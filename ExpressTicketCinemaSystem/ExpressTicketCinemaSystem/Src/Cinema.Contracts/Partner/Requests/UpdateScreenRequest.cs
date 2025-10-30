using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UpdateScreenRequest
    {
        [Required]
        public string ScreenName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string ScreenType { get; set; } = string.Empty;

        public string? SoundSystem { get; set; }

        [Range(1, 1000)]
        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}