namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class AdminGetUserByIdResponse
    {
        public string Message { get; set; } = string.Empty;
        public AdminUserDetailResponse Result { get; set; } = new();
    }

    public class AdminUserDetailResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string? Fullname { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvataUrl { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? BannedAt { get; set; }
        public DateTime? UnbannedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        // Password không được bao gồm trong response
    }
}