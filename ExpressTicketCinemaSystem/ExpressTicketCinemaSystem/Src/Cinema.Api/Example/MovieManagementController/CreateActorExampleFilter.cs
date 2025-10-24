using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class CreateActorExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "CreateActor")
            {
                return;
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Actor creation data";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Create Actor", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "name": "Tom Cruise",
                      "avatarUrl": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/3x4RETvagsVLEBi1K9hRqBXEqrC.jpg"
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
                      "message": "Tạo diễn viên thành công",
                      "result": {
                        "id": 3,
                        "name": "Tom Cruise",
                        "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/3x4RETvagsVLEBi1K9hRqBXEqrC.jpg"
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
                        "name": {
                          "msg": "Diễn viên với tên này đã tồn tại trong hệ thống",
                          "path": "name"
                        }
                      }
                    }
                    """
                        )
                    });
                }
            }
            // Thêm vào CreateActorExampleFilter
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
                "name": {
                  "msg": "Tên diễn viên là bắt buộc",
                  "path": "name"
                },
                "avatarUrl": {
                  "msg": "URL avatar không hợp lệ",
                  "path": "avatarUrl"
                }
              }
            }
            """
                        )
                    });
                }
            }

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
                "manager": {
                  "msg": "Manager không tồn tại hoặc không có quyền",
                  "path": "managerId",
                  "location": "auth"
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
              "message": "Đã xảy ra lỗi hệ thống khi tạo diễn viên."
            }
            """
                        )
                    });
                }
            }
            operation.Summary = "Create new actor";
            operation.Description = "Create a new actor in the system.";
        }
    }
}