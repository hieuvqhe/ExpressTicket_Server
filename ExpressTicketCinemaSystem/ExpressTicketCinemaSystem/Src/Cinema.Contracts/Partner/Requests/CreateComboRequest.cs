namespace ExpressTicketCinemaSystem.Src.Cinema.Application.DTO.Partner.Requests
{
    public class CreateComboRequest
    {
        public int CinemaId { get; set; }

        /// <summary>
        /// Tên combo (ví dụ: Combo bắp nước, Combo gia đình, v.v.)
        /// </summary>
        public string ComboName { get; set; } = null!;

        /// <summary>
        /// Giá combo
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Trạng thái khả dụng (mặc định true khi tạo mới)
        /// </summary>
        public bool IsAvailable { get; set; } = true;
    }
}
