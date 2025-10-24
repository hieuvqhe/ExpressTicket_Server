using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests;

public class CreateMovieRequest
{
    [Required(ErrorMessage = "Tiêu đề phim là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề phim không được vượt quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thể loại phim là bắt buộc")]
    public string Genre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thời lượng phim là bắt buộc")]
    [Range(1, 500, ErrorMessage = "Thời lượng phim phải từ 1 đến 500 phút")]
    public int DurationMinutes { get; set; }

    [Required(ErrorMessage = "Đạo diễn là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên đạo diễn không được vượt quá 100 ký tự")]
    public string Director { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngôn ngữ là bắt buộc")]
    public string Language { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quốc gia là bắt buộc")]
    public string Country { get; set; } = string.Empty;

    public string? PosterUrl { get; set; }

    [Required(ErrorMessage = "Nhà sản xuất là bắt buộc")]
    public string Production { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả phim là bắt buộc")]
    [StringLength(2000, ErrorMessage = "Mô tả phim không được vượt quá 2000 ký tự")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày công chiếu là bắt buộc")]
    public DateOnly PremiereDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    public DateOnly EndDate { get; set; }

    public string? TrailerUrl { get; set; }
    [Range(0, 10, ErrorMessage = "Điểm đánh giá chuyên gia phải từ 0 đến 10")]
    public decimal? AverageRating { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng đánh giá không thể âm")]
    public int? RatingsCount { get; set; }

    // Actors: có thể chọn actor có sẵn hoặc tạo mới
    public List<int>? ActorIds { get; set; } // Actor có sẵn
    public List<CreateActorInMovieRequest>? NewActors { get; set; } // Tạo actor mới
    public Dictionary<int, string>? ActorRoles { get; set; } // Vai diễn cho từng actor
}

public class UpdateMovieRequest
{
    [StringLength(200, ErrorMessage = "Tiêu đề phim không được vượt quá 200 ký tự")]
    public string? Title { get; set; }

    public string? Genre { get; set; }

    [Range(1, 500, ErrorMessage = "Thời lượng phim phải từ 1 đến 500 phút")]
    public int? DurationMinutes { get; set; }

    [StringLength(100, ErrorMessage = "Tên đạo diễn không được vượt quá 100 ký tự")]
    public string? Director { get; set; }

    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? PosterUrl { get; set; }
    public string? Production { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả phim không được vượt quá 2000 ký tự")]
    public string? Description { get; set; }

    public DateOnly? PremiereDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? TrailerUrl { get; set; }
    public bool? IsActive { get; set; }
    [Range(0, 10, ErrorMessage = "Điểm đánh giá chuyên gia phải từ 0 đến 10")]
    public decimal? AverageRating { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng đánh giá không thể âm")]
    public int? RatingsCount { get; set; }

    // Actors update
    public List<int>? ActorIds { get; set; }
    public List<CreateActorInMovieRequest>? NewActors { get; set; }
    public Dictionary<int, string>? ActorRoles { get; set; }
}

public class CreateActorInMovieRequest
{
    [Required(ErrorMessage = "Tên diễn viên là bắt buộc")]
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Diễn viên";
}