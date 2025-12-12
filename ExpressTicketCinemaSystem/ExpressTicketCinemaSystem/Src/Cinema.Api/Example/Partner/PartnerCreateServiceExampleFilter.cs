using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerCreateServiceExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            if (controllerName != "Partners" || actionName != "CreateService") return;

            // ===== Request Body =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Create Combo Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "name": "Combo Bắp + Nước",
                          "code": "COMBO_POPCORN_DRINK",
                          "price": 79000,
                          "description": "1 bắp lớn + 1 nước ngọt 22oz",
                          "imageUrl": "https://cdn.example.com/images/combo-popcorn-drink.jpg"
                        }
                        """
                        )
                    });
                }
            }

            // ===== 200 OK =====
            if (operation.Responses.ContainsKey("200"))
            {
                var resp = operation.Responses["200"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Tạo combo thành công",
                          "result": {
                            "serviceId": 101,
                            "partnerId": 1,
                            "name": "Combo Bắp + Nước",
                            "code": "COMBO_POPCORN_DRINK",
                            "price": 79000,
                            "isAvailable": true,
                            "description": "1 bắp lớn + 1 nước ngọt 22oz",
                            "imageUrl": "https://cdn.example.com/images/combo-popcorn-drink.jpg",
                            "createdAt": "2025-11-05T02:30:00Z",
                            "updatedAt": "2025-11-05T02:30:00Z"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 400 Bad Request (Validation) =====
            if (operation.Responses.ContainsKey("400"))
            {
                var resp = operation.Responses["400"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();

                    content.Examples.Add("Missing Name", new OpenApiExample
                    {
                        Summary = "Thiếu tên combo",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "name": {
                              "msg": "Tên combo là bắt buộc và tối đa 255 ký tự",
                              "path": "name",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Invalid Code", new OpenApiExample
                    {
                        Summary = "Mã combo sai định dạng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "code": {
                              "msg": "Mã combo chỉ gồm chữ cái, số, gạch dưới và tối đa 50 ký tự",
                              "path": "code",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Negative Price", new OpenApiExample
                    {
                        Summary = "Giá âm",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "price": {
                              "msg": "Giá phải >= 0",
                              "path": "price",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Invalid ImageUrl", new OpenApiExample
                    {
                        Summary = "URL ảnh không hợp lệ/không phải ảnh",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "imageUrl": {
                              "msg": "ImageUrl phải là URL http/https hợp lệ và là định dạng ảnh (jpg, jpeg, png, webp, svg)",
                              "path": "imageUrl",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 401 Unauthorized =====
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
                      "message": "Chỉ tài khoản Partner mới được sử dụng chức năng này",
                      "errors": {}
                    }
                    """
                    )
                });
            }

            // ===== 409 Conflict (duplicate code) =====
            if (operation.Responses.ContainsKey("409"))
            {
                var resp = operation.Responses["409"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Duplicate Code", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Xung đột dữ liệu",
                      "errors": {
                        "code": {
                          "msg": "Mã combo đã tồn tại trong hệ thống của bạn",
                          "path": "code",
                          "location": "body"
                        }
                      }
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
                      "message": "Đã xảy ra lỗi hệ thống khi tạo combo."
                    }
                    """
                    )
                });
            }
        }
    }
}
