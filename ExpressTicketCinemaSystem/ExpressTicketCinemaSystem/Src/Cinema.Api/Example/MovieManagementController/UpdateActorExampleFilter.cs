using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.MovieManagement
{
    public class UpdateActorExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "MovieManagement" || actionName != "UpdateActor")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
            if (idParam != null)
            {
                idParam.Description = "Actor ID";
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
                operation.RequestBody.Description = "Actor update data";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Actor", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                    {
                      "name": "Keanu Charles Reeves",
                      "avatarUrl": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg"
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
                      "message": "Cập nhật diễn viên thành công",
                      "result": {
                        "id": 1,
                        "name": "Keanu Charles Reeves",
                        "profileImage": "https://www.themoviedb.org/t/p/w300_and_h450_bestv2/4D0PpNI0kmP58hgrwGC3wCjxhnm.jpg"
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
            // Thêm vào UpdateActorExampleFilter
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
              "message": "Không tìm thấy diễn viên với ID này."
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
              "message": "Đã xảy ra lỗi hệ thống khi cập nhật diễn viên."
            }
            """
                        )
                    });
                }
            }
            operation.Summary = "Update actor information";
            operation.Description = "Update actor details including name and avatar.";
        }
    }
}