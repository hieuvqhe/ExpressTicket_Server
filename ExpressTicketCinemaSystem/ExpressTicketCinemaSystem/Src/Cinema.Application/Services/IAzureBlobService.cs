using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IAzureBlobService
    {
        Task<GeneratePdfUploadUrlResponse> GeneratePdfUploadUrlAsync(string fileName);
        Task<string> GeneratePdfReadUrlAsync(string blobUrl, int expiryInDays = 7); // Thêm dòng này
        Task<bool> DeletePdfAsync(string blobUrl);
    }
}