using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserChangePasswordExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "ChangePassword")
            {
                return;
            }

            // Request Body
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "oldPassword": "Password123!",
                          "newPassword": "NewPassword456!",
                          "confirmPassword": "NewPassword456!"
                        }
                        """
                        )
                    }
                }
            };

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
                          "message": "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
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
                    content.Examples.Add("Password Mismatch", new OpenApiExample
                    {
                        Summary = "Mật khẩu mới không khớp",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "confirmPassword": {
                              "msg": "Mật khẩu mới và xác nhận mật khẩu không khớp.",
                              "path": "confirmPassword",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Weak New Password", new OpenApiExample
                    {
                        Summary = "Mật khẩu mới yếu",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "newPassword": {
                              "msg": "Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt (6–12 ký tự).",
                              "path": "newPassword",
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
                            "form": {
                              "msg": "Vui lòng cung cấp mật khẩu cũ, mật khẩu mới và xác nhận mật khẩu.",
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

            // Response 401 Unauthorized
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Wrong Old Password", new OpenApiExample
                    {
                        Summary = "Sai mật khẩu cũ",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Mật khẩu cũ không chính xác.",
                               "path": "form",
                               "location": "body"
                             }
                           }
                         }
                         """
                         )
                    });
                    content.Examples.Add("Account Locked", new OpenApiExample
                    {
                        Summary = "Tài khoản bị khóa",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Tài khoản đã bị khóa.",
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

            // Response 404 Not Found
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("User Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Người dùng không tồn tại."
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
                          "message": "Đã xảy ra lỗi hệ thống"
                          
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}