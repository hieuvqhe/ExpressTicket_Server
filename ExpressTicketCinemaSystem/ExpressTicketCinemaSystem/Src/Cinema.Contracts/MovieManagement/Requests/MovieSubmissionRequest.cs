using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests
{
    public class CreateMovieSubmissionRequest
    {
        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Genre { get; set; } = string.Empty;

        [Range(1, 500)]
        public int DurationMinutes { get; set; }

        [Required, StringLength(100)]
        public string Director { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Language { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Country { get; set; } = string.Empty;

        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp)$", ErrorMessage = "Poster phải là ảnh (jpg, jpeg, png, webp)")]
        public string? PosterUrl { get; set; }

        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp)$", ErrorMessage = "Banner phải là ảnh (jpg, jpeg, png, webp)")]
        public string? BannerUrl { get; set; }

        [StringLength(100)]
        public string? Production { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        // DB là DATE NOT NULL => dùng DateOnly (bắt buộc)
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        [Required, FutureDate(ErrorMessage = "Ngày công chiếu phải ở tương lai")]
        public DateOnly PremiereDate { get; set; }

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        [Required, DateAfter(nameof(PremiereDate), ErrorMessage = "Ngày kết thúc phải sau ngày công chiếu")]
        public DateOnly EndDate { get; set; }

        [Url, RegularExpression(@"youtu\.?be", ErrorMessage = "Trailer phải là URL YouTube")]
        public string? TrailerUrl { get; set; }

        [Required, Url, RegularExpression(@"\.(jpg|jpeg|png|webp|pdf)$", ErrorMessage = "Tài liệu bản quyền phải là ảnh (jpg, jpeg, png, webp) hoặc PDF")]
        public string? CopyrightDocumentUrl { get; set; }

        [Required, Url, RegularExpression(@"\.(jpg|jpeg|png|webp|pdf)$", ErrorMessage = "Giấy phép phân phối phải là ảnh (jpg, jpeg, png, webp) hoặc PDF")]
        public string? DistributionLicenseUrl { get; set; }

        [StringLength(1000)]
        public string? AdditionalNotes { get; set; }

        // Tuỳ chọn đính kèm diễn viên ngay khi tạo
        public List<int>? ActorIds { get; set; } = new();
        public List<NewActorRequest>? NewActors { get; set; } = new();
        public Dictionary<int, string>? ActorRoles { get; set; } = new();
    }

    public class UpdateMovieSubmissionRequest
    {
        [StringLength(255)] public string? Title { get; set; }
        [StringLength(100)] public string? Genre { get; set; }
        [Range(1, 500)] public int? DurationMinutes { get; set; }
        [StringLength(100)] public string? Director { get; set; }
        [StringLength(50)] public string? Language { get; set; }
        [StringLength(50)] public string? Country { get; set; }

        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp)$")] public string? PosterUrl { get; set; }
        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp)$")] public string? BannerUrl { get; set; }

        [StringLength(100)] public string? Production { get; set; }
        [StringLength(2000)] public string? Description { get; set; }

        [JsonConverter(typeof(NullableDateOnlyJsonConverter))]
        [FutureDate(ErrorMessage = "Ngày công chiếu phải ở tương lai")]
        public DateOnly? PremiereDate { get; set; }

        [JsonConverter(typeof(NullableDateOnlyJsonConverter))]
        [DateAfter(nameof(PremiereDate), ErrorMessage = "Ngày kết thúc phải sau ngày công chiếu")]
        public DateOnly? EndDate { get; set; }

        [Url, RegularExpression(@"youtu\.?be")] public string? TrailerUrl { get; set; }
        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp|pdf)$")] public string? CopyrightDocumentUrl { get; set; }
        [Url, RegularExpression(@"\.(jpg|jpeg|png|webp|pdf)$")] public string? DistributionLicenseUrl { get; set; }
        [StringLength(1000)] public string? AdditionalNotes { get; set; }
    }
    public class NewActorRequest
    {
        [Required(ErrorMessage = "Tên diễn viên mới là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên diễn viên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Url(ErrorMessage = "URL avatar không hợp lệ")]
        [RegularExpression(@"\.(jpg|jpeg|png|webp)$", ErrorMessage = "Avatar phải là ảnh (jpg, jpeg, png, webp)")]
        public string? AvatarUrl { get; set; }

        [Required(ErrorMessage = "Vai diễn là bắt buộc")]
        [StringLength(100, ErrorMessage = "Vai diễn không được vượt quá 100 ký tự")]
        public string Role { get; set; } = string.Empty;
    }
}
