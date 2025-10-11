using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example
{
    public class AddGenericMovieExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Lấy tên Controller của API hiện tại
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];

            // Chỉ áp dụng nếu API này thuộc về "MoviesController"
            if (controllerName != "Movies")
            {
                return; // Bỏ qua nếu không phải
            }

            // Tìm đến response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;

                if (content != null)
                {
                    // Xóa các ví dụ cũ
                    content.Examples.Clear();

                    // Thêm ví dụ mới từ chuỗi JSON bạn cung cấp
                    content.Examples.Add("Default Example", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Get movies success",
                          "result": {
                            "movies": [
                              {
                                "movieId": 1,
                                "title": "The Shawshank Redemption",
                                "genre": "Drama",
                                "durationMinutes": 142,
                                "premiereDate": "2025-09-15",
                                "endDate": "2025-09-27",
                                "director": "Frank Darabont",
                                "language": "English",
                                "country": "USA",
                                "isActive": true,
                                "posterUrl": "https://www.themoviedb.org/t/p/w600_and_h900_bestv2/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg",
                                "production": "Castle Rock Entertainment",
                                "description": "Bộ phim tù nhân cảm động theo chân Andy Dufresne...",
                                "status": "end",
                                "trailerUrl": "https://www.youtube.com/watch?v=PLl99DlL6b4",
                                "actor": [
                                  {
                                    "id": 1,
                                    "name": "Tim Robbins",
                                    "profileImage": "https://media.themoviedb.org/t/p/w300_and_h450_bestv2/djLVFETFTvPyVUdrd7aLVykobof.jpg"
                                  },
                                  {
                                    "id": 2,
                                    "name": "Morgan Freeman",
                                    "profileImage": "https://media.themoviedb.org/t/p/w300_and_h450_bestv2/jPsLqiYGSofU4s6BjrxnefMfabb.jpg"
                                  }
                                ],
                                "averageRating": 9.3,
                                "ratingsCount": 381,
                                "createdAt": "2025-10-11T13:38:58.0066667",
                                "createdBy": null,
                                "updateAt": null
                              }
                            ],
                            "total": 1,
                            "page": 1,
                            "limit": 10,
                            "totalPages": 1
                          }
                        }
                        """
                        )
                    });
                }
            }

        }
    }
}
