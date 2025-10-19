using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class SendContractPdfRequest
    {
        [Required(ErrorMessage = "PDF URL là bắt buộc")]
        [Url(ErrorMessage = "PDF URL không hợp lệ")]
        public string PdfUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
