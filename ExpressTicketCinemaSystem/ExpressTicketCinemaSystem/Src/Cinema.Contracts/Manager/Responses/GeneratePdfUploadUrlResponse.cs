namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses
{
    public class GeneratePdfUploadUrlResponse
    {
        public string SasUrl { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
