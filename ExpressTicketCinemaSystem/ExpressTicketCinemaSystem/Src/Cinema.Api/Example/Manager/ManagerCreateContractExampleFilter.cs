using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerCreateContractExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "CreateContract")
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
                          "partnerId": 1,
                          "contractNumber": "HD-2024-001",
                          "contractType": "partnership",
                          "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                          "description": "Hợp đồng hợp tác cung cấp dịch vụ vé xem phim",
                          "termsAndConditions": "ĐIỀU 1: PHẠM VI HỢP TÁC...\nĐIỀU 2: QUYỀN VÀ NGHĨA VỤ...\nĐIỀU 3: ĐIỀU KHOẢN TÀI CHÍNH...",
                          "startDate": "2024-02-01",
                          "endDate": "2024-12-31",
                          "commissionRate": 15.5,
                          "minimumRevenue": 100000000
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
                          "message": "Tạo hợp đồng draft thành công. Dữ liệu đã sẵn sàng để tạo PDF.",
                          "result": {
                            "contractId": 1,
                            "contractNumber": "HD-2024-001",
                            "contractType": "partnership",
                            "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                            "description": "Hợp đồng hợp tác cung cấp dịch vụ vé xem phim",
                            "termsAndConditions": "ĐIỀU 1: PHẠM VI HỢP TÁC...",
                            "startDate": "2024-02-01",
                            "endDate": "2024-12-31",
                            "commissionRate": 15.5,
                            "minimumRevenue": 100000000,
                            "status": "draft",
                            "contractHash": "abc123hash",
                            "createdAt": "2024-01-15T10:00:00Z",
                            "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                            "partnerAddress": "123 Đường XYZ, Quận 1, TP.HCM",
                            "partnerTaxCode": "0123456789",
                            "partnerRepresentative": "Nguyễn Văn A",
                            "partnerPosition": "Đại diện hợp pháp",
                            "partnerEmail": "partner@example.com",
                            "partnerPhone": "0912345678",
                            "companyName": "CÔNG TY TNHH EXPRESS TICKET CINEMA SYSTEM",
                            "companyAddress": "123 Đường ABC, Quận 1, TP.HCM",
                            "companyTaxCode": "0312345678",
                            "managerName": "Trần Văn B",
                            "managerPosition": "Quản lý Đối tác",
                            "managerEmail": "manager@example.com"
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
                            "partnerId": {
                              "msg": "Partner chưa được duyệt, không thể tạo hợp đồng.",
                              "path": "partnerId",
                              "location": "body"
                            },
                            "contractNumber": {
                              "msg": "Số hợp đồng đã tồn tại trong hệ thống",
                              "path": "contractNumber",
                              "location": "body"
                            },
                            "commissionRate": {
                              "msg": "Tỷ lệ hoa hồng không thể vượt quá 50%",
                              "path": "commissionRate",
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
                    content.Examples.Add("Conflict Error", new OpenApiExample
                    {
                        Summary = "Xung đột dữ liệu",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "contractNumber": {
                              "msg": "Số hợp đồng đã tồn tại trong hệ thống",
                              "path": "contractNumber",
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
                          "message": "Không tìm thấy partner với ID này."
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
                          "message": "Đã xảy ra lỗi hệ thống khi tạo hợp đồng.",
                          "detail": "Database connection failed"
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}
