using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class CreateCinemaExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "CreateCinema")
            {
                return;
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Create cinema request";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Create Cinema", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "cinemaName": "Lotte Mỹ Đình",
                          "address": "Tầng 4, Lotte Center Hà Nội",
                          "phone": "024 3456 7890",
                          "code": "LOTTE_MD",
                          "city": "Hà Nội",
                          "district": "Nam Từ Liêm",
                          "latitude": 21.01667000,
                          "longitude": 105.78333000,
                          "email": "lotte.mydinh@lotte.vn",
                          "logoUrl": "https://example.com/logo/lotte_md.jpg"
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
                          "message": "Tạo rạp thành công",
                          "result": {
                            "cinemaId": 7,
                            "partnerId": 15,
                            "cinemaName": "Lotte Mỹ Đình",
                            "address": "Tầng 4, Lotte Center Hà Nội",
                            "phone": "024 3456 7890",
                            "code": "LOTTE_MD",
                            "city": "Hà Nội",
                            "district": "Nam Từ Liêm",
                            "latitude": 21.01667000,
                            "longitude": 105.78333000,
                            "email": "lotte.mydinh@lotte.vn",
                            "isActive": true,
                            "logoUrl": "https://example.com/logo/lotte_md.jpg",
                            "createdAt": "2025-10-28T14:30:00Z",
                            "updatedAt": "2025-10-28T14:30:00Z",
                            "totalScreens": 0,
                            "activeScreens": 0
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
                    content.Examples.Add("Validation Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "cinemaName": {
                              "msg": "Tên rạp là bắt buộc",
                              "path": "cinemaName"
                            },
                            "code": {
                              "msg": "Mã rạp chỉ được chứa chữ cái, số và dấu gạch dưới",
                              "path": "code"
                            },
                            "city": {
                              "msg": "Thành phố là bắt buộc",
                              "path": "city"
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
                            "contract": {
                              "msg": "Partner chưa có hợp đồng active hoặc hợp đồng đã hết hạn",
                              "path": "contract",
                              "location": "authorization"
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
                    content.Examples.Add("Conflict", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "code": {
                              "msg": "Mã rạp đã tồn tại trong hệ thống của bạn",
                              "path": "code"
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
                          "message": "Đã xảy ra lỗi hệ thống khi tạo rạp."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}