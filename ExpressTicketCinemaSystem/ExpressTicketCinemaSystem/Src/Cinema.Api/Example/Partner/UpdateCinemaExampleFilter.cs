using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class UpdateCinemaExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UpdateCinema")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var cinemaIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "cinema_id");
            if (cinemaIdParam != null)
            {
                cinemaIdParam.Description = "Cinema ID";
                cinemaIdParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample { Value = new OpenApiString("4") }
                };
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Update cinema request";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Cinema", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "cinemaName": "Lotte Hòa Lạc (Updated)",
                          "address": "Khu CNC Hòa Lạc, Km29 Đại lộ Thăng Long",
                          "phone": "024 1234 9999",
                          "city": "Hà Nội",
                          "district": "Thạch Thất",
                          "latitude": 21.02013000,
                          "longitude": 105.51616000,
                          "email": "info@lottehoalac.vn",
                          "logoUrl": "https://example.com/logo/lotte_hl_updated.jpg",
                          "isActive": true
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
                          "message": "Cập nhật rạp thành công",
                          "result": {
                            "cinemaId": 4,
                            "partnerId": 15,
                            "cinemaName": "Lotte Hòa Lạc (Updated)",
                            "address": "Khu CNC Hòa Lạc, Km29 Đại lộ Thăng Long",
                            "phone": "024 1234 9999",
                            "code": "LOTTE_HL",
                            "city": "Hà Nội",
                            "district": "Thạch Thất",
                            "latitude": 21.02013000,
                            "longitude": 105.51616000,
                            "email": "info@lottehoalac.vn",
                            "isActive": true,
                            "logoUrl": "https://example.com/logo/lotte_hl_updated.jpg",
                            "createdAt": "2025-10-28T11:48:05.7633333Z",
                            "updatedAt": "2025-10-28T15:45:00Z",
                            "totalScreens": 3,
                            "activeScreens": 3
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
                              "msg": "Tên rạp không được vượt quá 255 ký tự",
                              "path": "cinemaName"
                            },
                            "isActive": {
                              "msg": "Không thể vô hiệu hóa rạp đang có 3 phòng chiếu đang hoạt động",
                              "path": "isActive"
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
                            "access": {
                              "msg": "Tài khoản partner đã bị vô hiệu hóa",
                              "path": "authorization",
                              "location": "header"
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
                          "message": "Không tìm thấy rạp với ID này hoặc không thuộc quyền quản lý của bạn"
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
                          "message": "Đã xảy ra lỗi hệ thống khi cập nhật rạp."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}