using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserSubmitNewEmailExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "SubmitNewEmail")
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
                          "newEmail": "new.email@example.com"
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
                          "message": "Mã xác minh đã được gửi tới email mới.",
                          "result": {
                             "requestId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                             "expiresAt": "2025-10-19T10:50:00Z",
                             "currentVerified": true
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
                    content.Examples.Add("Invalid New Email Format", new OpenApiExample
                    {
                        Summary = "Email mới sai định dạng",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Lỗi xác thực dữ liệu",
                           "errors": {
                             "newEmail": {
                               "msg": "Định dạng email mới không hợp lệ.",
                               "path": "newEmail",
                               "location": "body"
                             }
                           }
                         }
                         """
                        )
                    });
                    content.Examples.Add("Missing Fields", new OpenApiExample
                    {
                        Summary = "Thiếu trường",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Lỗi xác thực dữ liệu",
                           "errors": {
                             "requestId": {
                               "msg": "Thiếu requestId.",
                               "path": "requestId",
                               "location": "body"
                             },
                             "newEmail": {
                               "msg": "NewEmail không được rỗng.",
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

            // Response 401 Unauthorized
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Current Not Verified", new OpenApiExample
                    {
                        Summary = "Email cũ chưa xác thực",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Chưa xác thực email hiện tại.",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Request Expired", new OpenApiExample
                    {
                        Summary = "Yêu cầu hết hạn",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Yêu cầu đã hết hạn. Vui lòng tạo lại yêu cầu.",
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
                        Summary = "Email mới đã tồn tại",
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
                          "message": "Lỗi khi gửi mã tới email mới."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}