using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetServicesExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            if (controllerName != "Partners" || actionName != "GetServices") return;

            // ===== Parameters examples =====
            operation.Parameters ??= new List<OpenApiParameter>();

            var parametersToUpdate = new[]
            {
                new { Name = "page",        Example = "1",  Description = "Page number (default: 1)" },
                new { Name = "limit",       Example = "10", Description = "Number of items per page (default: 10)" },
                new { Name = "search",      Example = "combo", Description = "Search by name or code" },
                new { Name = "is_available",Example = "true", Description = "Filter by availability (true/false)" },
                new { Name = "sort_by",     Example = "created_at", Description = "Sort by: created_at | name | price | code" },
                new { Name = "sort_order",  Example = "desc", Description = "Sort order: asc | desc" }
            };

            foreach (var param in parametersToUpdate)
            {
                var p = operation.Parameters.FirstOrDefault(x => x.Name == param.Name);
                if (p != null)
                {
                    p.Description = param.Description;
                    p.Examples = new Dictionary<string, OpenApiExample>
                    {
                        ["Example"] = new OpenApiExample { Value = new OpenApiString(param.Example) }
                    };
                }
            }

            // ===== 200 OK =====
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
                          "message": "Lấy danh sách combo thành công",
                          "result": {
                            "services": [
                              {
                                "serviceId": 101,
                                "partnerId": 1,
                                "name": "Combo Bắp + Nước",
                                "code": "COMBO_POPCORN_DRINK",
                                "price": 79000,
                                "isAvailable": true,
                                "description": "1 bắp lớn + 1 nước ngọt 22oz",
                                "imageUrl": "https://cdn.example.com/images/combo-popcorn-drink.jpg",
                                "createdAt": "2025-11-01T08:00:00Z",
                                "updatedAt": "2025-11-05T02:30:00Z"
                              },
                              {
                                "serviceId": 102,
                                "partnerId": 1,
                                "name": "Combo Gia Đình",
                                "code": "COMBO_FAMILY",
                                "price": 149000,
                                "isAvailable": false,
                                "description": "2 bắp lớn + 2 nước 22oz",
                                "imageUrl": "https://cdn.example.com/images/combo-family.jpg",
                                "createdAt": "2025-10-20T02:00:00Z",
                                "updatedAt": "2025-11-02T02:00:00Z"
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

            // ===== 400 Bad Request (Validation for page/limit) =====
            if (operation.Responses.ContainsKey("400"))
            {
                var resp = operation.Responses["400"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Invalid Pagination", new OpenApiExample
                    {
                        Summary = "Tham số phân trang không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "page": {
                              "msg": "Số trang phải lớn hơn 0",
                              "path": "page",
                              "location": "query"
                            },
                            "limit": {
                              "msg": "Số lượng mỗi trang phải từ 1 đến 100",
                              "path": "limit",
                              "location": "query"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 401 =====
            if (operation.Responses.ContainsKey("401"))
            {
                var resp = operation.Responses["401"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Unauthorized", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Xác thực thất bại",
                      "errors": {}
                    }
                    """
                    )
                });
            }

            // ===== 500 =====
            if (operation.Responses.ContainsKey("500"))
            {
                var resp = operation.Responses["500"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Server Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách combo."
                    }
                    """
                    )
                });
            }
        }
    }
}
