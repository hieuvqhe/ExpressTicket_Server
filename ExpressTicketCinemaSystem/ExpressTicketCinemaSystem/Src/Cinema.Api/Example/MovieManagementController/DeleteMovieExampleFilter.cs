using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class DeleteMovieExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "DeleteMovie")
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
                      "message": "Xóa phim thành công"
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
                          "msg": "Không thể xóa phim đã có lịch chiếu",
                          "path": "movieId"
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
            operation.Summary = "Delete movie (soft delete)";
            operation.Description = "Soft delete movie by setting IsActive = false (only allowed for movies without showtimes).";
        }
    }
}