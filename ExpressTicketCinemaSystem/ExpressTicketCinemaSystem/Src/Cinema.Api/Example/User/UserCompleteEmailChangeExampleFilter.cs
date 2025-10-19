using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserCompleteEmailChangeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "CompleteEmailChange")
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
                          "requestId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
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
                          "message": "Thay đổi email thành công."
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
                    content.Examples.Add("New Email Not Set", new OpenApiExample
                    {
                        Summary = "Email mới chưa được thiết lập",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Lỗi xác thực dữ liệu",
                           "errors": {
                             "request": {
                               "msg": "Email mới chưa được thiết lập trên request.",
                               "path": "request",
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
                    content.Examples.Add("Not Verified", new OpenApiExample
                    {
                        Summary = "Chưa xác thực đủ 2 email",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Cần xác nhận cả email hiện tại và email mới trước khi hoàn tất.",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Expired", new OpenApiExample
                    {
                        Summary = "Một trong 2 mã hết hạn",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Một trong các mã xác thực đã hết hạn.",
                               "path": "form",
                               "location": "body"
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
                    content.Examples.Add("New Email Exists", new OpenApiExample
                    {
                        Summary = "Email mới đã bị dùng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xung đột dữ liệu",
                          "errors": {
                            "newEmail": {
                              "msg": "Email mới đã được sử dụng bởi tài khoản khác.",
                              "path": "newEmail",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 404 Not Found (RequestId)
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
                          "message": "Lỗi khi hoàn tất đổi email."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}