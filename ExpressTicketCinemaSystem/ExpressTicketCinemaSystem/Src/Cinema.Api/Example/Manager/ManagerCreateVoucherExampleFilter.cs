using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerCreateVoucherExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "ManagerVoucher" || actionName != "CreateVoucher")
            {
                return;
            }

            // Request Body
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "voucherCode": "SUMMER2025",
                          "discountType": "percent",
                          "discountVal": 15.0,
                          "validFrom": "2025-06-01",
                          "validTo": "2025-08-31",
                          "usageLimit": 1000,
                          "description": "Giảm 15% cho mùa hè 2025",
                          "isActive": true
                        }
                        """
                        )
                    }
                }
            };

            // Response 201 Created
            if (operation.Responses.ContainsKey("201"))
            {
                var response = operation.Responses["201"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Tạo voucher thành công",
                          "result": {
                            "voucherId": 1,
                            "voucherCode": "SUMMER2025",
                            "discountType": "percent",
                            "discountVal": 15.0,
                            "validFrom": "2025-06-01",
                            "validTo": "2025-08-31",
                            "usageLimit": 1000,
                            "usedCount": 0,
                            "description": "Giảm 15% cho mùa hè 2025",
                            "isActive": true,
                            "createdAt": "2024-01-15T10:00:00Z",
                            "updatedAt": null,
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
                            "discountVal": {
                              "msg": "Giá trị giảm giá phần trăm không được vượt quá 100",
                              "path": "discountVal",
                              "location": "body"
                            },
                            "validTo": {
                              "msg": "Ngày kết thúc phải sau ngày bắt đầu",
                              "path": "validTo",
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
                          "message": "Manager không tồn tại"
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