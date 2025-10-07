using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public int CinemaId { get; set; }

    public string ServiceName { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
}
