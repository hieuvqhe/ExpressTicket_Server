using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Auth
{
    public class AuthResendVerificationExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Auth" || actionName != "ResendVerification")
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
                          "email": "nguyenvanb@example.com"
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
                          "message": "Email xác minh đã được gửi lại."
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
                    content.Examples.Add("User Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy user",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "email": {
                              "msg": "Không tìm thấy người dùng với email này.",
                              "path": "email",
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
                    content.Examples.Add("Already Verified", new OpenApiExample
                    {
                        Summary = "Email đã xác minh",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "email": {
                              "msg": "Email đã được xác minh.",
                              "path": "email",
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