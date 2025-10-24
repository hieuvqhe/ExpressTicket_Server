using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class GetScreensExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetScreens")
            {
                return;
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
                          "message": "Get screens thành công",
                          "result": {
                            "screens": [
                              {
                                "screen_id": 1,
                                "cinema_id": 1,
                                "name": "Screen 1",
                                "seat_layout": [
                                  [
                                    {
                                      "row": "A",
                                      "number": 1,
                                      "type": "regular",
                                      "status": "active"
                                    },
                                    {
                                      "row": "A",
                                      "number": 2,
                                      "type": "regular",
                                      "status": "active"
                                    }
                                  ],
                                  [
                                    {
                                      "row": "B",
                                      "number": 1,
                                      "type": "vip",
                                      "status": "active"
                                    },
                                    {
                                      "row": "B",
                                      "number": 2,
                                      "type": "vip",
                                      "status": "active"
                                    }
                                  ]
                                ],
                                "capacity": 150,
                                "screen_type": "standard",
                                "status": "active",
                                "created_at": "2025-10-24T03:58:05.481Z",
                                "updated_at": "2025-10-24T03:58:05.481Z"
                              }
                            ],
                            "total": 1,
                            "page": 1,
                            "limit": 10,
                            "total_pages": 1
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
                    content.Examples.Add("Not Owner", new OpenApiExample
                    {
                        Summary = "Không phải chủ sở hữu",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Bạn không có quyền truy cập screen này.",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Not Approved", new OpenApiExample
                    {
                        Summary = "Partner chưa duyệt",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Tài khoản partner chưa được approved.",
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
                    content.Examples.Add("Screen Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy screen với ID này."
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin screen."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}