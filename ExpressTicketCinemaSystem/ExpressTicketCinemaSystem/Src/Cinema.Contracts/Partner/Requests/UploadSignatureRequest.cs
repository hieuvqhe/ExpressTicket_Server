namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{
    public class UploadSignatureRequest
    {
        public string SignedContractPdfUrl { get; set; } = string.Empty; 
        public string? Notes { get; set; } 
    }
}