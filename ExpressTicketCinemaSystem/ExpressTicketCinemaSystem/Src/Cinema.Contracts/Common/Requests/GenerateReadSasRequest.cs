namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Requests
{
    public class GenerateReadSasRequest
    {
        /// <summary>
        /// Blob URL thuần được lưu trong DB (không có SAS)
        /// </summary>
        public string BlobUrl { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian hiệu lực của SAS (tính theo ngày). Mặc định 7 ngày nếu gửi <= 0.
        /// </summary>
        public int ExpiryInDays { get; set; } = 7;
    }
}



































