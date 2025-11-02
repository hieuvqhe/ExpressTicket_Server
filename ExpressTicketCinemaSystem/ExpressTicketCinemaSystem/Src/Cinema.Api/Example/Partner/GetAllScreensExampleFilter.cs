using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class GetAllScreensExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetScreens")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var parameters = new[]
            {
                new { Name = "cinema_id", Example = "4", Description = "Cinema ID" },
                new { Name = "page", Example = "1", Description = "Page number (default: 1)" },
                new { Name = "limit", Example = "10", Description = "Items per page (default: 10)" },
                new { Name = "screen_type", Example = "standard", Description = "Filter by screen type" },
                new { Name = "is_active", Example = "true", Description = "Filter by active status" },
                new { Name = "sort_by", Example = "screen_name", Description = "Sort field" },
                new { Name = "sort_order", Example = "asc", Description = "Sort order" }
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
                          "message": "Lấy danh sách phòng thành công",
                          "result": {
                            "screens": [
                              {
                                "screenId": 1,
                                "cinemaId": 4,
                                "cinemaName": "Lotte Hòa Lạc",
                                "screenName": "Phòng 1 - Standard",
                                "code": "LOTTE_HL_P1",
                                "description": "Phòng chiếu tiêu chuẩn với âm thanh Dolby Digital",
                                "screenType": "standard",
                                "soundSystem": "Dolby Digital",
                                "capacity": 120,
                                "seatRows": 10,
                                "seatColumns": 12,
                                "isActive": true,
                                "hasSeatLayout": false,
                                "createdDate": "2024-01-15T08:00:00Z",
                                "updatedDate": "2024-01-15T08:00:00Z"
                              },
                              {
                                "screenId": 2,
                                "cinemaId": 4,
                                "cinemaName": "Lotte Hòa Lạc",
                                "screenName": "Phòng 2 - IMAX",
                                "code": "LOTTE_HL_P2",
                                "description": "Phòng IMAX với màn hình cực lớn và âm thanh vòm",
                                "screenType": "imax",
                                "soundSystem": "Dolby Atmos",
                                "capacity": 200,
                                "seatRows": 12,
                                "seatColumns": 17,
                                "isActive": true,
                                "hasSeatLayout": true,
                                "createdDate": "2024-01-16T09:00:00Z",
                                "updatedDate": "2024-01-20T10:00:00Z"
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách phòng."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}