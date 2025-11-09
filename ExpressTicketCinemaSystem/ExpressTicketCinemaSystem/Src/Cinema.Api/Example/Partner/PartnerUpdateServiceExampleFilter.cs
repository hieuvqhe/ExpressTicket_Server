using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerUpdateServiceExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            if (controllerName != "Partners" || actionName != "UpdateService") return;

            // ===== Request Body =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Combo Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "name": "Combo Bắp + Nước (Mới)",
                          "price": 85000,
                          "description": "1 bắp lớn + 1 nước 22oz (refill 1 lần)",
                          "imageUrl": "https://cdn.example.com/images/combo-popcorn-drink-v2.jpg",
                          "isAvailable": true
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
                          "message": "Cập nhật combo thành công",
                          "result": {
                            "serviceId": 101,
                            "partnerId": 1,
                            "name": "Combo Bắp + Nước (Mới)",
                            "code": "COMBO_POPCORN_DRINK",
                            "price": 85000,
                            "isAvailable": true,
                            "description": "1 bắp lớn + 1 nước 22oz (refill 1 lần)",
                            "imageUrl": "https://cdn.example.com/images/combo-popcorn-drink-v2.jpg",
                            "createdAt": "2025-11-01T08:00:00Z",
                            "updatedAt": "2025-11-05T03:00:00Z"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 400 Bad Request =====
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

            // ===== 404 =====
            if (operation.Responses.ContainsKey("404"))
            {
                var resp = operation.Responses["404"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Not Found", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Không tìm thấy combo với ID này hoặc không thuộc quyền quản lý của bạn"
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
                      "message": "Đã xảy ra lỗi hệ thống khi cập nhật combo."
                    }
                    """
                    )
                });
            }
        }
    }
}
