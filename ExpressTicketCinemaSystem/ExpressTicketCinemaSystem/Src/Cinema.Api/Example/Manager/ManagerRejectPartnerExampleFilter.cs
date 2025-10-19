using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerRejectPartnerExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "RejectPartner")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
            if (idParam != null)
            {
                idParam.Description = "Partner ID";
                idParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(1)
                    }
                };
            }

            // Request Body example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Reject partner request";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Reject Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "rejectionReason": "Giấy tờ kinh doanh không đầy đủ. Vui lòng cung cấp giấy phép kinh doanh và giấy đăng ký thuế bản gốc."
                        }
                        """
                        )
                    });
                }
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
                          "message": "Từ chối partner thành công.",
                          "result": {
                            "partnerId": 1,
                            "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                            "taxCode": "0123456789",
                            "status": "rejected",
                            "rejectionReason": "Giấy tờ kinh doanh không đầy đủ. Vui lòng cung cấp giấy phép kinh doanh và giấy đăng ký thuế bản gốc.",
                            "rejectedAt": "2024-01-20T10:00:00Z",
                            "rejectedBy": 1,
                            "managerName": "Trần Văn B",
                            "userId": 101,
                            "fullname": "Nguyễn Văn A",
                            "email": "nguyenvana@example.com",
                            "phone": "0912345678",
                            "isActive": false,
                            "emailConfirmed": false
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
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "rejectionReason": {
                              "msg": "Lý do từ chối là bắt buộc",
                              "path": "rejectionReason"
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
                          "message": "Không tìm thấy partner với ID này."
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
                    content.Examples.Add("Conflict", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "status": {
                              "msg": "Chỉ có thể từ chối partner với trạng thái 'pending'. Hiện tại: approved",
                              "path": "status"
                            }
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
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi từ chối partner."
                        }
                        """
                        )
                    });
                }
            }

            operation.Summary = "Reject a pending partner";
            operation.Description = "Reject partner registration with detailed reason and send notification email.";
        }
    }
}