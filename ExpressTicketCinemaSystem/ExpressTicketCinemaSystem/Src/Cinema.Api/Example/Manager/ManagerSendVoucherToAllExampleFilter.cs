using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerSendVoucherToAllExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "ManagerVoucher" || actionName != "SendVoucherToAllUsers")
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
                          "subject": "Đừng Bỏ Lỡ Voucher Giảm Giá Dành Cho Bạn",
                          "customMessage": "Chào bạn, hệ thống gửi tặng bạn voucher đặc biệt cho mùa hè này. Đây là ưu đãi dành cho tất cả người dùng trung thành của chúng tôi. Hãy nhanh tay sử dụng!"
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
                          "message": "Đã gửi voucher thành công đến 150 users, thất bại: 2",
                          "result": {
                            "totalSent": 150,
                            "totalFailed": 2,
                            "results": [
                              {
                                "userEmail": "user1@example.com",
                                "userName": "Nguyễn Văn A",
                                "success": true,
                                "sentAt": "2024-01-15T10:00:00Z"
                              },
                              {
                                "userEmail": "user2@example.com", 
                                "userName": "Trần Thị B",
                                "success": false,
                                "errorMessage": "Email không tồn tại",
                                "sentAt": "2024-01-15T10:00:01Z"
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
                            "subject": {
                              "msg": "Tiêu đề email là bắt buộc",
                              "path": "subject",
                              "location": "body"
                            },
                            "customMessage": {
                              "msg": "Nội dung email là bắt buộc", 
                              "path": "customMessage",
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