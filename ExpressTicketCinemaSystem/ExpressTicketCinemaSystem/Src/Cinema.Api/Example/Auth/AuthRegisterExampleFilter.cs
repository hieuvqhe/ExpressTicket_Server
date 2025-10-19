using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Auth
{
    public class AuthRegisterExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Auth" || actionName != "Register")
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
                          "fullName": "Nguyễn Văn B",
                        "username": "nguyenvanb",
                        "email": "nguyenvanb@example.com",
                        "password": "Password123!",
                        "confirmPassword": "Password123!"
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
                          "message": "Đăng ký thành công. Vui lòng kiểm tra email để xác minh.",
                          "result": {
                            "fullname": "Nguyễn Văn B",
                            "username": "nguyenvanb",
                            "email": "nguyenvanb@example.com",
                            "emailConfirmed": false
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
                    content.Examples.Add("Invalid Email", new OpenApiExample
                    {
                        Summary = "Email sai định dạng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "email": {
                              "msg": "Định dạng email không hợp lệ.",
                              "path": "email",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Weak Password", new OpenApiExample
                    {
                        Summary = "Mật khẩu yếu",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "password": {
                              "msg": "Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).",
                              "path": "password",
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
                    content.Examples.Add("Email Exists", new OpenApiExample
                    {
                        Summary = "Email đã tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "email": {
                              "msg": "Email đã tồn tại.",
                              "path": "email",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Username Exists", new OpenApiExample
                    {
                        Summary = "Username đã tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "username": {
                              "msg": "Tên đăng nhập đã tồn tại.",
                              "path": "username",
                              "location": "body"
                            }
                          }
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
                          "message": "Đã xảy ra lỗi hệ thống trong quá trình đăng ký."
                          
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}