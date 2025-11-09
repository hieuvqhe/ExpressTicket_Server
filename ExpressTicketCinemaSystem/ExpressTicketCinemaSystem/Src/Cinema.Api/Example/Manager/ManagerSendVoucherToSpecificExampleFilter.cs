using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerSendVoucherToSpecificExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "ManagerVoucher" || actionName != "SendVoucherToSpecificUsers")
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
                          "subject": "Đừng Bỏ Lỡ Voucher Đặc Biệt Dành Cho Bạn ",
                          "customMessage": "Chào bạn, chúng tôi gửi tặng bạn voucher đặc biệt này như một lời cảm ơn vì đã là khách hàng thân thiết. Ưu đãi này chỉ dành riêng cho bạn!",
                          "userIds": [1, 2, 3, 4, 5]
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
                          "message": "Đã gửi voucher thành công đến 4 users, thất bại: 1",
                          "result": {
                            "totalSent": 4,
                            "totalFailed": 1,
                            "results": [
                              {
                                "userEmail": "customer1@example.com",
                                "userName": "Lê Văn C",
                                "success": true,
                                "sentAt": "2024-01-15T10:00:00Z"
                              },
                              {
                                "userEmail": "customer2@example.com",
                                "userName": "Phạm Thị D", 
                                "success": true,
                                "sentAt": "2024-01-15T10:00:01Z"
                              },
                              {
                                "userEmail": "customer3@example.com",
                                "userName": "Hoàng Văn E",
                                "success": false,
                                "errorMessage": "Email không tồn tại",
                                "sentAt": "2024-01-15T10:00:02Z"
                              }
                            ]
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
                            "userIds": {
                              "msg": "Danh sách user không được để trống",
                              "path": "userIds",
                              "location": "body"
                            },
                            "subject": {
                              "msg": "Tiêu đề email là bắt buộc",
                              "path": "subject",
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
                        Summary = "Lỗi xác thực",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Bạn không có quyền gửi voucher này",
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
                        Summary = "Không tìm thấy",
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
                        Summary = "Lỗi server",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi gửi voucher"
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}