using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests
{
    public class AddActorToSubmissionRequest
    {
        public int? ActorId { get; set; }         
        public string? ActorName { get; set; }     
        public string? ActorAvatarUrl { get; set; }
        public string Role { get; set; } = "Diễn viên";
    }
    public class UpdateSubmissionActorRequest
    {
        public string Role { get; set; }
        public string? ActorName { get; set; } // Chỉ cập nhật nếu là actor mới
        public string? ActorAvatarUrl { get; set; }
    }
}