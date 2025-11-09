using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Realtime
{
    // Dùng trong "snapshot" ban đầu
    public class SnapshotPayload
    {
        public DateTime ServerTime { get; set; }
        public List<SeatCell> Seats { get; set; } = new();
    }

    // Dùng cho các delta: seat_locked / seat_released / seat_sold
    public class SeatDeltaPayload
    {
        public int SeatId { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    // Dùng cho "heartbeat"
    public class HeartbeatPayload
    {
        public DateTime ServerTime { get; set; }
    }

    // Mô tả 1 ô ghế trong snapshot
    public class SeatCell
    {
        public int SeatId { get; set; }
        public string RowCode { get; set; } = "";
        public int SeatNumber { get; set; }
        public int? SeatTypeId { get; set; }
        // AVAILABLE | LOCKED | SOLD | BLOCKED
        public string Status { get; set; } = "AVAILABLE";
        public DateTime? LockedUntil { get; set; }
    }
}
