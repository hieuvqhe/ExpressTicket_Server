using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class UpdateMovieExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "UpdateMovie")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
            if (idParam != null)
            {
                idParam.Description = "Movie ID";
                idParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(1)
                    }
                };
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Movie update data";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Movie", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "title": "The Matrix Resurrections (Updated)",
                      "description": "Updated description with more details...",
                      "averageRating": 9.0,
                      "ratingsCount": 20,
                      "isActive": true
                    }
                    """
                        )
                    });
                }
            }

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "message": "Cập nhật phim thành công",
                      "result": {
                        "movieId": 1,
                        "title": "The Matrix Resurrections (Updated)",
                        "genre": "Sci-Fi, Action",
                        "durationMinutes": 148,
                        "premiereDate": "2025-03-22",
                        "endDate": "2025-04-22",
                        "director": "Lana Wachowski",
                        "language": "English",
                        "country": "USA",
                        "isActive": true,
                        "posterUrl": "https://www.themoviedb.org/t/p/w600_and_h900_bestv2/8c4a8kE7PizaKQQpvzKu6M2L1Vj.jpg",
                        "production": "Warner Bros. Pictures",
                        "description": "Updated description with more details...",
                        "status": "upcoming",
                        "trailerUrl": "https://www.youtube.com/watch?v=9ix7TUGVYIo",
                        "averageRating": 9.0,
                        "ratingsCount": 20,
                        "createdAt": "2024-01-24T10:30:00Z",
                        "createdBy": "Nguyễn Văn A",
                        "updateAt": "2024-01-24T11:30:00Z",
                        "actor": [
                          {
                            "id": 1,
                            "name": "Keanu Reeves",
                            "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg"
                          }
                        ]
                      }
                    }
                    """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Validation Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "message": "Lỗi xác thực dữ liệu",
                      "errors": {
                        "movie": {
                          "msg": "Không thể cập nhật phim đã có lịch chiếu",
                          "path": "movieId"
                        }
                      }
                    }
                    """
                        )
                    });
                }
            }
            // Thêm vào UpdateMovieExampleFilter
            // Response 401 Unauthorized
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Unauthorized", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
            {
              "message": "Xác thực thất bại",
              "errors": {
                "access": {
                  "msg": "Bạn không có quyền chỉnh sửa phim này",
                  "path": "movieId",
                  "location": "path"
                }
              }
            }
            """
                        )
                    });
                }
            }

            // Response 404 Not Found
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
            {
              "message": "Không tìm thấy phim với ID này."
            }
            """
                        )
                    });
                }
            }

            // Response 409 Conflict
            if (operation.Responses.ContainsKey("409"))
            {
                var response = operation.Responses["409"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Conflict", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
            {
              "message": "Dữ liệu bị xung đột",
              "errors": {
                "premiereDate": {
                  "msg": "Không thể thay đổi ngày công chiếu cho phim đã/đang chiếu",
                  "path": "premiereDate",
                  "location": "body"
                }
              }
            }
            """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
            {
              "message": "Đã xảy ra lỗi hệ thống khi cập nhật phim."
            }
            """
                        )
                    });
                }
            }
            operation.Summary = "Update movie information";
            operation.Description = "Update movie details (only allowed for movies without showtimes).";
        }
    }
}