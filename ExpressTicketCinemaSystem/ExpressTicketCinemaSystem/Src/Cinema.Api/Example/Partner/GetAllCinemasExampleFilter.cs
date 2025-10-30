using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class GetAllCinemasExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetCinemas")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var parameters = new[]
            {
                new { Name = "page", Example = "1", Description = "Page number (default: 1)" },
                new { Name = "limit", Example = "10", Description = "Items per page (default: 10)" },
                new { Name = "city", Example = "Hà Nội", Description = "Filter by city" },
                new { Name = "district", Example = "Thạch Thất", Description = "Filter by district" },
                new { Name = "isActive", Example = "true", Description = "Filter by active status" },
                new { Name = "search", Example = "Lotte", Description = "Search term" },
                new { Name = "sortBy", Example = "cinema_name", Description = "Sort field" },
                new { Name = "sortOrder", Example = "asc", Description = "Sort order" }
            };

            foreach (var param in parameters)
            {
                var existingParam = operation.Parameters.FirstOrDefault(p => p.Name == param.Name);
                if (existingParam != null)
                {
                    existingParam.Description = param.Description;
                    existingParam.Examples = new Dictionary<string, OpenApiExample>
                    {
                        ["Example"] = new OpenApiExample { Value = new OpenApiString(param.Example) }
                    };
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
                          "message": "Lấy danh sách rạp thành công",
                          "result": {
                            "cinemas": [
                              {
                                "cinemaId": 4,
                                "partnerId": 15,
                                "cinemaName": "Lotte Hòa Lạc",
                                "address": "Khu CNC Hòa Lạc",
                                "phone": "024 1234 5678",
                                "code": "LOTTE_HL",
                                "city": "Hà Nội",
                                "district": "Thạch Thất",
                                "latitude": 21.02013000,
                                "longitude": 105.51616000,
                                "email": "lotte.hoalac@lotte.vn",
                                "isActive": true,
                                "logoUrl": "https://example.com/logo/lotte_hl.jpg",
                                "createdAt": "2025-10-28T11:48:05.7633333Z",
                                "updatedAt": "2025-10-28T11:48:05.7633333Z",
                                "totalScreens": 3,
                                "activeScreens": 3
                              },
                              {
                                "cinemaId": 5,
                                "partnerId": 15,
                                "cinemaName": "CGV Long Biên",
                                "address": "Tầng 4, Vincom Long Biên",
                                "phone": "024 2345 6789",
                                "code": "CGV_LB",
                                "city": "Hà Nội",
                                "district": "Long Biên",
                                "latitude": 21.04022000,
                                "longitude": 105.89437000,
                                "email": "cgv.longbien@cgv.vn",
                                "isActive": true,
                                "logoUrl": "https://example.com/logo/cgv_lb.jpg",
                                "createdAt": "2025-10-28T11:48:05.7633333Z",
                                "updatedAt": "2025-10-28T11:48:05.7633333Z",
                                "totalScreens": 3,
                                "activeScreens": 2
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 2,
                              "totalPages": 1
                            }
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
                            "page": {
                              "msg": "Số trang phải lớn hơn 0",
                              "path": "page"
                            },
                            "limit": {
                              "msg": "Số lượng mỗi trang phải từ 1 đến 100",
                              "path": "limit"
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
                              "msg": "Chỉ tài khoản Partner mới được sử dụng chức năng này",
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách rạp."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}