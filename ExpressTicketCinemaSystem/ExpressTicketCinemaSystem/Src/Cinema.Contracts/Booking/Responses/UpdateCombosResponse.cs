namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Responses
{
    public class UpdateCombosResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        /// <summary>Tổng đơn vị sau khi lưu (sum duplicated ids)</summary>
        public int TotalUnits { get; set; }
        /// <summary>Mảng serviceId đã lưu (lặp theo quantity)</summary>
        public List<int> ComboIds { get; set; } = new();
    }

    public class RemoveComboResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public int RemovedServiceId { get; set; }
        public int TotalUnits { get; set; }
        public List<int> ComboIds { get; set; } = new();
    }

    public class SetVoucherResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public string VoucherCode { get; set; } = null!;
    }

    public class RemoveVoucherResponse
    {
        public Guid BookingSessionId { get; set; }
        public int ShowtimeId { get; set; }
        public string Message { get; set; } = "Đã gỡ voucher khỏi session";
    }
}
