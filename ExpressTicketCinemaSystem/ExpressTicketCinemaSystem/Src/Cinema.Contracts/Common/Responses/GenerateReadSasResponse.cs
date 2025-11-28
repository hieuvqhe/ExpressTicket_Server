namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses
{
    public class GenerateReadSasResponse
    {
        /// <summary>
        /// URL có chữ ký (SAS) dùng để đọc file PDF / blob
        /// </summary>
        public string ReadSasUrl { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm hết hạn của SAS
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}



































