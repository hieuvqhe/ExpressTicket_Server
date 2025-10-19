using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class PartnerGetContractByIdExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

        if (controllerName != "Partners" || actionName != "GetContractById")
        {
            return;
        }

        // Parameters examples
        operation.Parameters ??= new List<OpenApiParameter>();

        var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
        if (idParam != null)
        {
            idParam.Description = "Contract ID";
            idParam.Examples = new Dictionary<string, OpenApiExample>
            {
                ["Example"] = new OpenApiExample
                {
                    Value = new OpenApiString("1")
                }
            };
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
                      "message": "Lấy thông tin hợp đồng thành công",
                      "result": {
                        "contractId": 1,
                        "managerId": 1,
                        "partnerId": 1,
                        "createdBy": 1,
                        "contractNumber": "HD-2024-001",
                        "contractType": "partnership",
                        "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                        "description": "Hợp đồng hợp tác cung cấp dịch vụ vé xem phim",
                        "termsAndConditions": "ĐIỀU 1: PHẠM VI HỢP TÁC...\nĐIỀU 2: QUYỀN VÀ NGHĨA VỤ...",
                        "startDate": "2024-02-01",
                        "endDate": "2024-12-31",
                        "commissionRate": 15.5,
                        "minimumRevenue": 100000000,
                        "status": "active",
                        "isLocked": true,
                        "isActive": true,
                        "contractHash": "abc123hash",
                        "partnerSignatureUrl": "https://example.com/signatures/partner-signature-123.jpg",
                        "managerSignature": "manager_digital_signature_abc123",
                        "signedAt": "2024-01-20T10:00:00Z",
                        "partnerSignedAt": "2024-01-18T15:30:00Z",
                        "managerSignedAt": "2024-01-20T10:00:00Z",
                        "lockedAt": "2024-01-20T10:00:00Z",
                        "createdAt": "2024-01-15T08:00:00Z",
                        "updatedAt": "2024-01-20T10:00:00Z",
                        "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                        "partnerAddress": "123 Đường XYZ, Quận 1, TP.HCM",
                        "partnerTaxCode": "0123456789",
                        "partnerRepresentative": "Nguyễn Văn A",
                        "partnerPosition": "Đại diện hợp pháp",
                        "partnerEmail": "partner@example.com",
                        "partnerPhone": "0912345678",
                        "managerName": "Trần Văn B",
                        "managerPosition": "Quản lý Đối tác",
                        "managerEmail": "manager@example.com",
                        "createdByName": "Trần Văn B",
                        "companyName": "CÔNG TY TNHH EXPRESS TICKET CINEMA SYSTEM",
                        "companyAddress": "123 Đường ABC, Quận 1, TP.HCM",
                        "companyTaxCode": "0312345678"
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
                        "access": {
                          "msg": "Bạn không có quyền xem hợp đồng này",
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
    }
}