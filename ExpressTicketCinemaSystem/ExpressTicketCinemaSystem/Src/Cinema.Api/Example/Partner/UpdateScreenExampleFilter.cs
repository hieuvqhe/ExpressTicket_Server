using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class UpdateScreenExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UpdateScreen")
            {
                return;
            }

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
                          "name": "Screen 1 Updated",
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
                          "capacity": 160,
                          "screen_type": "premium",
                          "status": "active"
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
                          "message": "Update thành công",
                          "result": {
                            "screen_id": 1
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

                    content.Examples.Add("Required Fields", new OpenApiExample
                    {
                        Summary = "Thiếu trường bắt buộc",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "name": {
                              "msg": "Tên screen là bắt buộc",
                              "path": "name",
                              "location": "body"
                            },
                            "screen_type": {
                              "msg": "Loại screen là bắt buộc",
                              "path": "screen_type",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Invalid Screen Type", new OpenApiExample
                    {
                        Summary = "Loại screen không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "screen_type_invalid": {
                              "msg": "Loại screen không hợp lệ. Chỉ chấp nhận: standard, premium, imax, 3d, 4dx",
                              "path": "screen_type",
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
                    content.Examples.Add("Duplicate Screen Name", new OpenApiExample
                    {
                        Summary = "Tên screen đã tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "name": {
                              "msg": "Tên screen 'Screen 1 Updated' đã tồn tại trong cinema này",
                              "path": "name",
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
                          "message": "Đã xảy ra lỗi hệ thống khi tạo screen."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}