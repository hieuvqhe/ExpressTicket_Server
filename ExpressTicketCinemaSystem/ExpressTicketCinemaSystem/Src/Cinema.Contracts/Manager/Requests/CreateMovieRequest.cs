using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    public class CreateMovieRequest
    {
        [Required(ErrorMessage = "Tiêu đề phim là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Thể loại phim là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Thể loại không được vượt quá 100 ký tự")]
        public string Genre { get; set; } = null!;

        [Required(ErrorMessage = "Thời lượng phim là bắt buộc")]
        [Range(1, 300, ErrorMessage = "Thời lượng phải từ 1 đến 300 phút")]
        public int DurationMinutes { get; set; }

        [MaxLength(255, ErrorMessage = "Tên đạo diễn không được vượt quá 255 ký tự")]
        public string? Director { get; set; }

        [MaxLength(50, ErrorMessage = "Ngôn ngữ không được vượt quá 50 ký tự")]
        public string? Language { get; set; }

        [MaxLength(100, ErrorMessage = "Quốc gia không được vượt quá 100 ký tự")]
        public string? Country { get; set; }

        public string? PosterUrl { get; set; }

        [MaxLength(255, ErrorMessage = "Nhà sản xuất không được vượt quá 255 ký tự")]
        public string? Production { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày công chiếu là bắt buộc")]
        public DateOnly PremiereDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateOnly EndDate { get; set; }

        [MaxLength(500, ErrorMessage = "URL trailer không được vượt quá 500 ký tự")]
        public string? TrailerUrl { get; set; }

        public List<MovieActorRequest> Actors { get; set; } = new List<MovieActorRequest>();
        public class MovieActorRequest
        {
            public int? ActorId { get; set; }  // Nullable: nếu có thì chọn từ dropdown
            public string? Name { get; set; }   // Required nếu ActorId null (tạo mới)
            public string? AvatarUrl { get; set; }
            public string Role { get; set; } = "Actor";
        }
    }
}