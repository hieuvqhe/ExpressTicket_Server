using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserGetVouchersExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "UserVoucher")
            {
                return;
            }

            // Xử lý cho endpoint GetValidVouchers
            if (actionName == "GetValidVouchers")
            {
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
                              "message": "Tìm thấy 3 voucher hợp lệ",
                              "result": [
                                {
                                  "voucherId": 1,
                                  "voucherCode": "SUMMER2025",
                                  "discountType": "percent",
                                  "discountVal": 15.0,
                                  "validFrom": "2025-06-01",
                                  "validTo": "2025-08-31",
                                  "usageLimit": 1000,
                                  "usedCount": 150,
                                  "description": "Giảm 15% cho mùa hè 2025",
                                  "createdAt": "2024-01-15T10:00:00Z",
                                  "discountText": "15%",
                                  "isExpired": false,
                                  "isAvailable": true,
                                  "remainingUses": 850
                                },
                                {
                                  "voucherId": 2,
                                  "voucherCode": "WELCOME50K",
                                  "discountType": "fixed",
                                  "discountVal": 50000.0,
                                  "validFrom": "2024-01-01",
                                  "validTo": "2024-12-31",
                                  "usageLimit": 500,
                                  "usedCount": 320,
                                  "description": "Giảm 50,000 VNĐ cho đơn hàng đầu tiên",
                                  "createdAt": "2024-01-10T08:30:00Z",
                                  "discountText": "50,000 VNĐ",
                                  "isExpired": false,
                                  "isAvailable": true,
                                  "remainingUses": 180
                                }
                              ]
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
                                "token": ["Token không hợp lệ hoặc không chứa ID người dùng."]
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
                        content.Examples.Add("ServerError", new OpenApiExample
                        {
                            Value = new OpenApiString(
                            """
                            {
                              "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách voucher"
                            }
                            """
                            )
                        });
                    }
                }
            }

            // Xử lý cho endpoint GetVoucherById
            if (actionName == "GetVoucherById")
            {
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
                              "message": "Lấy thông tin voucher thành công",
                              "result": {
                                "voucherId": 1,
                                "voucherCode": "SUMMER2025",
                                "discountType": "percent",
                                "discountVal": 15.0,
                                "validFrom": "2025-06-01",
                                "validTo": "2025-08-31",
                                "usageLimit": 1000,
                                "usedCount": 150,
                                "description": "Giảm 15% cho mùa hè 2025",
                                "createdAt": "2024-01-15T10:00:00Z",
                                "discountText": "15%",
                                "isExpired": false,
                                "isAvailable": true,
                                "remainingUses": 850
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
                        content.Examples.Add("NotFound", new OpenApiExample
                        {
                            Value = new OpenApiString(
                            """
                            {
                              "message": "Không tìm thấy voucher hợp lệ"
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
                                "token": ["Token không hợp lệ hoặc không chứa ID người dùng."]
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
                        content.Examples.Add("ServerError", new OpenApiExample
                        {
                            Value = new OpenApiString(
                            """
                            {
                              "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin voucher"
                            }
                            """
                            )
                        });
                    }
                }
            }

            // Xử lý cho endpoint GetVoucherByCode
            if (actionName == "GetVoucherByCode")
            {
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
                              "message": "Lấy thông tin voucher thành công",
                              "result": {
                                "voucherId": 2,
                                "voucherCode": "WELCOME50K",
                                "discountType": "fixed",
                                "discountVal": 50000.0,
                                "validFrom": "2024-01-01",
                                "validTo": "2024-12-31",
                                "usageLimit": 500,
                                "usedCount": 320,
                                "description": "Giảm 50,000 VNĐ cho đơn hàng đầu tiên",
                                "createdAt": "2024-01-10T08:30:00Z",
                                "discountText": "50,000 VNĐ",
                                "isExpired": false,
                                "isAvailable": true,
                                "remainingUses": 180
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
                        content.Examples.Add("NotFound", new OpenApiExample
                        {
                            Value = new OpenApiString(
                            """
                            {
                              "message": "Không tìm thấy voucher hợp lệ"
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
                                "token": ["Token không hợp lệ hoặc không chứa ID người dùng."]
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
                        content.Examples.Add("ServerError", new OpenApiExample
                        {
                            Value = new OpenApiString(
                            """
                            {
                              "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin voucher"
                            }
                            """
                            )
                        });
                    }
                }
            }
        }
    }
}