using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerUpdateVoucherExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "ManagerVoucher" || actionName != "UpdateVoucher")
            {
                return;
            }

            // Request Body - UPDATED with all fields
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "voucherCode": "SUMMER2025_UPDATED",
                          "discountType": "percent",
                          "discountVal": 20.0,
                          "validFrom": "2025-06-01",
                          "validTo": "2025-09-30",
                          "usageLimit": 1500,
                          "description": "Giảm 20% cho mùa hè 2025 - Ưu đãi đặc biệt",
                          "isActive": true
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
                          "message": "Cập nhật voucher thành công",
                          "result": {
                            "voucherId": 1,
                            "voucherCode": "SUMMER2025_UPDATED",
                            "discountType": "percent",
                            "discountVal": 20.0,
                            "validFrom": "2025-06-01",
                            "validTo": "2025-09-30",
                            "usageLimit": 1500,
                            "usedCount": 0,
                            "description": "Giảm 20% cho mùa hè 2025 - Ưu đãi đặc biệt",
                            "isActive": true,
                            "createdAt": "2024-01-15T10:00:00Z",
                            "updatedAt": "2024-01-16T14:30:00Z",
                            "managerId": 1,
                            "managerName": "Trần Văn B"
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
                        Summary = "Lỗi validation",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "voucherCode": {
                              "msg": "Mã voucher đã tồn tại",
                              "path": "voucherCode",
                              "location": "body"
                            },
                            "validFrom": {
                              "msg": "Ngày bắt đầu không được ở quá khứ",
                              "path": "validFrom",
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
                    content.Examples.Add("Unauthorized", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Bạn không có quyền cập nhật voucher này",
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
                    content.Examples.Add("Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Voucher không tồn tại"
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
                          "message": "Đã xảy ra lỗi hệ thống khi tạo voucher"
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}