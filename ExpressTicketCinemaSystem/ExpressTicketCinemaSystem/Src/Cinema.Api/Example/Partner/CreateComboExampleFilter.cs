using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class CreateComboExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName == "Partner" && actionName == "CreateCombo")
            {
                ApplyCreateComboExamples(operation);
            }
            else if (controllerName == "Partner" && actionName == "DeleteCombo")
            {
                ApplyDeleteComboExamples(operation);
            }
        }

        private void ApplyCreateComboExamples(OpenApiOperation operation)
        {
            // Request Body Example
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "partnerId": 1,
                          "serviceName": "Combo bắp nước VIP",
                          "price": 120000
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
                        Summary = "Tạo combo thành công",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Tạo combo thành công",
                          "result": {
                            "serviceId": 10,
                            "serviceName": "Combo bắp nước VIP",
                            "price": 120000,
                            "isAvailable": true
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
                    content.Examples.Add("Invalid Partner", new OpenApiExample
                    {
                        Summary = "Partner chưa được duyệt hoặc không tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Partner chưa được duyệt, không thể tạo combo."
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
                    content.Examples.Add("Cinema Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy rạp của partner",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Partner chưa có rạp để tạo combo."
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
                          "message": "Đã xảy ra lỗi hệ thống khi tạo combo."
                        }
                        """
                        )
                    });
                }
            }
        }

        private void ApplyDeleteComboExamples(OpenApiOperation operation)
        {
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
                        Summary = "Xóa combo thành công (soft delete)",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xóa combo thành công",
                          "result": {
                            "serviceId": 10,
                            "isAvailable": false
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
                    content.Examples.Add("Combo Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy combo với ID này",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy combo."
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
                        Summary = "Lỗi hệ thống khi xóa combo",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi xóa combo."
                        }
                        """
                        )
                    });
                }
            }

            // Parameter Example
            if (operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters)
                {
                    if (parameter.Name == "comboId")
                    {
                        parameter.Description = "Combo ID (ServiceId)";
                        parameter.Required = true;
                        parameter.Example = new OpenApiInteger(10);
                    }
                }
            }
        }
    }
}
