namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests
{

    public class CreateServiceRequest
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateServiceRequest
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } // cho phép bật/tắt nhanh
    }

    public class GetServicesQuery
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Search { get; set; } // name/code
        public bool? IsAvailable { get; set; }
        public string SortBy { get; set; } = "created_at"; // created_at | name | price | code
        public string SortOrder { get; set; } = "desc";    // asc | desc
    }
}
