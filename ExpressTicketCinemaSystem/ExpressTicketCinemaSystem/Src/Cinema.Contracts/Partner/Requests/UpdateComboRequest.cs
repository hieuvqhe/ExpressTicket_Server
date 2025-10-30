namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Requests
{
    public class UpdateComboRequest
    {
        /// <summary>
        /// Tên combo mới
        /// </summary>
        public string ComboName { get; set; } = null!;

        /// <summary>
        /// Giá combo mới
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Cập nhật trạng thái khả dụng
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
