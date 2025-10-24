using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class ManagerFinalizeContractExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

        if (controllerName != "Manager" || actionName != "FinalizeContract")
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
                      "managerSignature": "manager_digital_signature_abc123",
                      "notes": "Hợp đồng đã được ký và xác nhận thành công"
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
                      "message": "Hợp đồng đã được khóa và hoàn tất thành công",
                      "result": {
                        "contractId": 1,
                        "contractNumber": "HD-2024-001",
                        "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                        "status": "active",
                        "isLocked": true,
                        "managerSignature": "manager_digital_signature_abc123",
                        "managerSignedAt": "2024-01-20T10:00:00Z",
                        "lockedAt": "2024-01-20T10:00:00Z",
                        "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                        "partnerEmail": "partner@example.com"
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
                        "managerSignature": {
                          "msg": "Chữ ký số của manager là bắt buộc",
                          "path": "managerSignature",
                          "location": "body"
                        },
                        "partnerSignature": {
                          "msg": "Partner chưa upload ảnh biên bản ký",
                          "path": "partnerSignature",
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
                "access": {
                  "msg": "Bạn không có quyền thao tác với hợp đồng này",
                  "path": "contractId",
                  "location": "path"
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
                    Summary = "Không tìm thấy hợp đồng",
                    Value = new OpenApiString(
                    """
            {
              "message": "Không tìm thấy hợp đồng với ID này."
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
                    Summary = "Lỗi hệ thống",
                    Value = new OpenApiString(
                    """
            {
              "message": "Đã xảy ra lỗi hệ thống khi hoàn tất hợp đồng."
            }
            """
                    )
                });
            }
        }
    }
}