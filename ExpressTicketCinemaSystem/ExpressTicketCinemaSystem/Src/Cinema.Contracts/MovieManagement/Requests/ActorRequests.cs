using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests
{
    public class CreateActorRequest
    {
        [Required(ErrorMessage = "Tên diễn viên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên diễn viên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }

    public class UpdateActorRequest
    {
        [Required(ErrorMessage = "Tên diễn viên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên diễn viên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }
}