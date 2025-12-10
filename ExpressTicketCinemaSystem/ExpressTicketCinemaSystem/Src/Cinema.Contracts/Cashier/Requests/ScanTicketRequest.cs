using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Cashier.Requests;

public class ScanTicketRequest
{
    [Required(ErrorMessage = "QR code không được để trống")]
    public string QrCode { get; set; } = null!;
}















