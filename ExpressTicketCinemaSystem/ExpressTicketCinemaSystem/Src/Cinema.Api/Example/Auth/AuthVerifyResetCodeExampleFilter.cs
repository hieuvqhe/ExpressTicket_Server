using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Auth
{
    public class AuthVerifyResetCodeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Auth" || actionName != "VerifyResetCode")
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
                          "emailOrUsername": "nguyenvanb@example.com",
                          "code": "123456"
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
                          "message": "Mã hợp lệ, bạn có thể đặt lại mật khẩu mới."
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
                    content.Examples.Add("Invalid Code", new OpenApiExample
                    {
                        Summary = "Mã không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực",
                          "errors": {
                            "code": {
                              "msg": "Mã không hợp lệ.",
                              "path": "code",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Expired Code", new OpenApiExample
                    {
                        Summary = "Mã hết hạn",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực",
                          "errors": {
                            "code": {
                              "msg": "Mã xác minh đã hết hạn, vui lòng yêu cầu mã mới.",
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