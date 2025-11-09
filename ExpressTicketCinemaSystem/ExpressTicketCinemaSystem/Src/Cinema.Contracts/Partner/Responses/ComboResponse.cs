using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses
{
    public class ServiceResponse
    {
        public int ServiceId { get; set; }
        public int PartnerId { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ServiceActionResponse
    {
        public int ServiceId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PaginatedServicesResponse
    {
        public List<ServiceResponse> Services { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = null!;
    }
}
