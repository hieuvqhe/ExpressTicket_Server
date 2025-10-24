using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class RejectPartnerRequest
    {
        [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Lý do từ chối không được vượt quá 1000 ký tự")]
        public string RejectionReason { get; set; } = string.Empty;

    }
}
