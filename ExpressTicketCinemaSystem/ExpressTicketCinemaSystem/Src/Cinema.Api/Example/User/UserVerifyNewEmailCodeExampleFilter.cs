using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserVerifyNewEmailCodeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "VerifyEmailChangeNew")
            {
                return;
            }

            // ==================== REQUEST BODY EXAMPLE ====================
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "requestId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                          "code": "654321" 
                        }
                        """
                        )
                    }
                }
            };

            // ==================== RESPONSE EXAMPLES ====================

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
                          "message": "Xác thực email mới thành công."
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
                    content.Examples.Add("Empty Code", new OpenApiExample
                    {
                        Summary = "Thiếu code",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Lỗi xác thực dữ liệu",
                           "errors": {
                             "code": {
                               "msg": "Code không được rỗng.",
                               "path": "code",
                               "location": "body"
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
                    content.Examples.Add("Expired Code", new OpenApiExample
                    {
                        Summary = "Mã hết hạn",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Mã xác thực email mới đã hết hạn.",
                               "path": "form",
                               "location": "body"
                             }
                           }
                         }
                         """
                        )
                    });
                    content.Examples.Add("Invalid Code", new OpenApiExample
                    {
                        Summary = "Mã không hợp lệ",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Mã xác thực không hợp lệ.",
                               "path": "form",
                               "location": "body"
                             }
                           }
                         }
                         """
                        )
                    });
                    // Thêm ví dụ cho token không hợp lệ từ GetCurrentUserId
                    content.Examples.Add("Invalid Token", new OpenApiExample { /* Tương tự GetMe */ });
                }
            }

            // Response 404 Not Found (RequestId không tồn tại)
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Request Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Yêu cầu đổi email không tồn tại hoặc đã được sử dụng."
                         }
                         """
                        )
                    });
                }
            }

            // Response 500
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
                          "message": "Lỗi khi xác thực mã email mới."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}