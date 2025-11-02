using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class CreateScreenRequest
    {
        [Required]
        public string ScreenName { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string ScreenType { get; set; } = string.Empty;

        public string? SoundSystem { get; set; }

        [Range(1, 1000)]
        public int Capacity { get; set; }

        [Range(1, 50)]
        public int SeatRows { get; set; }

        [Range(1, 30)]
        public int SeatColumns { get; set; }
    }
}