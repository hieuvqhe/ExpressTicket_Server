using Amazon.S3;
using Amazon.S3.Transfer;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _config;

        public S3Service(IAmazonS3 s3Client, IConfiguration config)
        {
            _s3Client = s3Client;
            _config = config;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string v)
        {
            var bucketName = _config["AWS:BucketName"];
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";

            using (var stream = file.OpenReadStream())
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = bucketName,
                    ContentType = file.ContentType
                };

                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            }

            return $"https://{bucketName}.s3.{_config["AWS:Region"]}.amazonaws.com/{fileName}";
        }

        //public async Task<string> UploadVideoAsync(IFormFile file)
        //{
        //    var bucketName = _config["AWS:BucketName"];
        //    var fileName = $"{Guid.NewGuid()}_{file.FileName}";

        //    using (var stream = file.OpenReadStream())
        //    {
        //        var uploadRequest = new TransferUtilityUploadRequest
        //        {
        //            InputStream = stream,
        //            Key = fileName,
        //            BucketName = bucketName,
        //            ContentType = file.ContentType
        //        };

        //        var fileTransferUtility = new TransferUtility(_s3Client);
        //        await fileTransferUtility.UploadAsync(uploadRequest);
        //    }

        //    return $"https://{bucketName}.s3.{_config["AWS:Region"]}.amazonaws.com/{fileName}";
        //}
    }
}
