using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using Microsoft.Extensions.Options;
using System; 
using System.IO; 
using System.Threading.Tasks; 

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly int _sasTokenExpiryMinutes;

        public AzureBlobService(IOptions<AzureBlobStorageSettings> options)
        {
            var settings = options.Value;
            _blobServiceClient = new BlobServiceClient(settings.ConnectionString);
            _containerName = settings.ContainerName;
            _sasTokenExpiryMinutes = settings.SasTokenExpiryMinutes;
        }

        public async Task<GeneratePdfUploadUrlResponse> GeneratePdfUploadUrlAsync(string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = uniqueFileName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_sasTokenExpiryMinutes)
                };

                // Cho phép vừa upload (write/create/add) vừa đọc lại file (read)
                // để FE có thể dùng cùng một SasUrl cho cả upload và download PDF.
                sasBuilder.SetPermissions(BlobSasPermissions.Write |
                                          BlobSasPermissions.Create |
                                          BlobSasPermissions.Add |
                                          BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                var blobUrl = blobClient.Uri.ToString(); 

                return new GeneratePdfUploadUrlResponse
                {
                    SasUrl = sasUri.ToString(), 
                    BlobUrl = blobUrl,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_sasTokenExpiryMinutes)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo SAS URL (Upload): {ex.Message}", ex);
            }
        }


        public async Task<string> GeneratePdfReadUrlAsync(string blobUrl, int expiryInDays = 7)
        {
            try
            {
                var blobUri = new Uri(blobUrl);
                var blobName = Path.GetFileName(blobUri.LocalPath); 

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException("Không tìm thấy blob.", blobName);
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(expiryInDays) 
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo SAS URL (Read): {ex.Message}", ex);
            }
        }
        public async Task<bool> DeletePdfAsync(string blobUrl)
        {
            try
            {

                var blobUri = new Uri(blobUrl);
                var blobName = Path.GetFileName(blobUri.LocalPath);

                if (string.IsNullOrEmpty(blobName))
                {
                    return false;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa file PDF: {ex.Message}", ex);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                            .Replace(" ", "_")
                            .ToLower();
        }
    }
}