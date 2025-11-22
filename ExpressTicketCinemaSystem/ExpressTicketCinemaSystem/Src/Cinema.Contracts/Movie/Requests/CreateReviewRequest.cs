using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Requests
{
    public class CreateReviewRequest
    {
        [Required(ErrorMessage = "Số sao đánh giá là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Số sao đánh giá phải từ 1 đến 5")]
        [JsonPropertyName("rating_star")]
        public int RatingStar { get; set; }

        [Required(ErrorMessage = "Bình luận là bắt buộc")]
        [MinLength(1, ErrorMessage = "Bình luận không được để trống")]
        [MaxLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự")]
        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [MaxLength(3, ErrorMessage = "Tối đa 3 ảnh được phép")]
        [JsonPropertyName("image_urls")]
        public List<string>? ImageUrls { get; set; }
    }
}

