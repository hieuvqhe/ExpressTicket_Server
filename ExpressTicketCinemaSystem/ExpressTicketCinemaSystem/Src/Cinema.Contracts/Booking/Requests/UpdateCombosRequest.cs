namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Booking.Requests
{
    public class UpdateCombosRequest
    {
        /// <summary>Tối đa 8 đơn vị tổng (sum qty <= 8)</summary>
        public List<UpdateCombosItem> Items { get; set; } = new();
    }

    public class UpdateCombosItem
    {
        public int ServiceId { get; set; }
        public int Quantity { get; set; }  // >=0 ; 0 thì coi như không thêm
    }

    public class SetVoucherRequest
    {
        /// <summary>Voucher code (in hoa/số). Yêu cầu user đã đăng nhập.</summary>
        public string VoucherCode { get; set; } = null!;
    }
}
