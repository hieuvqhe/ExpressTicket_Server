using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class CreateMovieExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "CreateMovie")
            {
                return;
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Movie creation data with actors";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Create Movie with Actors", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "title": "The Matrix Resurrections",
                          "genre": "Sci-Fi, Action",
                          "durationMinutes": 148,
                          "director": "Lana Wachowski",
                          "language": "English",
                          "country": "USA",
                          "posterUrl": "https://www.themoviedb.org/t/p/w600_and_h900_bestv2/8c4a8kE7PizaKQQpvzKu6M2L1Vj.jpg",
                          "production": "Warner Bros. Pictures",
                          "description": "Neo sống một cuộc sống bình thường dưới cái tên Thomas A. Anderson tại San Francisco. Anh gặp một phụ nữ trông giống Tiffany, người mà anh cảm thấy quen thuộc.",
                          "premiereDate": "2025-03-22",
                          "endDate": "2025-04-22",
                          "trailerUrl": "https://www.youtube.com/watch?v=9ix7TUGVYIo",
                          "averageRating": 8.5,
                          "ratingsCount": 15,
                          "actorIds": [1, 2],
                          "newActors": [
                            {
                              "name": "Keanu Reeves",
                              "avatarUrl": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg",
                              "role": "Neo / Thomas Anderson"
                            }
                          ],
                          "actorRoles": {
                            "1": "Nhân vật chính",
                            "2": "Nhân vật phụ"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 201 Created
            if (operation.Responses.ContainsKey("201"))
            {
                var response = operation.Responses["201"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Tạo phim thành công",
                          "result": {
                            "movieId": 1,
                            "title": "The Matrix Resurrections",
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
                            "description": "Neo sống một cuộc sống bình thường...",
                            "status": "upcoming",
                            "trailerUrl": "https://www.youtube.com/watch?v=9ix7TUGVYIo",
                            "averageRating": 8.5,
                            "ratingsCount": 15,
                            "createdAt": "2024-01-24T10:30:00Z",
                            "createdBy": "Nguyễn Văn A",
                            "updateAt": "2024-01-24T10:30:00Z",
                            "actor": [
                              {
                                "id": 1,
                                "name": "Keanu Reeves",
                                "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg"
                              },
                              {
                                "id": 2,
                                "name": "Carrie-Anne Moss",
                                "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/5gJGLnwO14V4aQKjdlL0VbJN9yO.jpg"
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
                            "premiereDate": {
                              "msg": "Ngày công chiếu không thể trong quá khứ",
                              "path": "premiereDate"
                            },
                            "durationMinutes": {
                              "msg": "Thời lượng phim phải từ 1 đến 500 phút",
                              "path": "durationMinutes"
                            }
                          }
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
                            "movie": {
                              "msg": "Đã tồn tại phim với cùng tiêu đề và ngày công chiếu",
                              "path": "title"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }
            // Thêm vào DeleteMovieExampleFilter
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
                  "msg": "Bạn không có quyền xóa phim này",
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
              "message": "Đã xảy ra lỗi hệ thống khi xóa phim."
            }
            """
                        )
                    });
                }
            }
            operation.Summary = "Create new movie";
            operation.Description = "Create a new movie with actor selection (existing or new actors).";
        }
    }
}