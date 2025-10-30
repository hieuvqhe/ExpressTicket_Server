using System;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models
{
    public partial class ServiceComponent
    {
        public int ComboServiceId { get; set; }

        public int ItemServiceId { get; set; }

        public int Quantity { get; set; } = 1;

        public string ComponentKind { get; set; } = "item";

        // --------------------- Navigation Properties ---------------------
        public virtual Service ComboService { get; set; } = null!;
        public virtual Service ItemService { get; set; } = null!;
    }
}
