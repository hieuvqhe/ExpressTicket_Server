using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models
{
    public partial class Service
    {
        public int ServiceId { get; set; }

        public int CinemaId { get; set; }

        public string ServiceName { get; set; } = null!;

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int PartnerId { get; set; }

        public string Type { get; set; } = "single";
        public string? ShortDesc { get; set; }
        public int SortOrder { get; set; }
        public string Category { get; set; } = "food";
        public DateTime? DeletedAt { get; set; }

        // ---------------- Relation with ServiceComponent ----------------
        public virtual Cinema Cinema { get; set; } = null!;
        public virtual ICollection<ServiceComponent> ComboComponents { get; set; } = new List<ServiceComponent>();
        public virtual ICollection<ServiceComponent> ItemComponents { get; set; } = new List<ServiceComponent>();
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}
