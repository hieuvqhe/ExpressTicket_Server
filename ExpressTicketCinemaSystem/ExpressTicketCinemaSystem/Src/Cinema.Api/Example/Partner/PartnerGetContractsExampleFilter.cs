using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetContractsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetPartnerContracts")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var parametersToAdd = new[]
            {
            new { Name = "page", Example = "1", Description = "Page number (default: 1)" },
            new { Name = "limit", Example = "10", Description = "Number of items per page (default: 10)" },
            new { Name = "status", Example = "active", Description = "Filter by contract status (draft, pending, active, expired)" },
            new { Name = "contractType", Example = "partnership", Description = "Filter by contract type" },
            new { Name = "search", Example = "HD-2024", Description = "Search term for contract number or title" },
            new { Name = "sortBy", Example = "created_at", Description = "Field to sort by" },
            new { Name = "sortOrder", Example = "desc", Description = "Sort order (asc, desc)" }
        };

            foreach (var param in parametersToAdd)
            {
                var existingParam = operation.Parameters.FirstOrDefault(p => p.Name == param.Name);
                if (existingParam != null)
                {
                    existingParam.Description = param.Description;
                    existingParam.Examples = new Dictionary<string, OpenApiExample>
                    {
                        ["Example"] = new OpenApiExample
                        {
                            Value = new OpenApiString(param.Example)
                        }
                    };
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
                      "message": "Lấy danh sách hợp đồng thành công",
                      "result": {
                        "contracts": [
                          {
                            "contractId": 1,
                            "contractNumber": "HD-2024-001",
                            "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                            "contractType": "partnership",
                            "startDate": "2024-02-01",
                            "endDate": "2024-12-31",
                            "commissionRate": 15.5,
                            "minimumRevenue": 100000000,
                            "status": "active",
                            "isLocked": true,
                            "isActive": true,
                            "partnerSignatureUrl": "https://example.com/signatures/partner-signature-123.jpg",
                            "managerSignature": "manager_digital_signature_abc123",
                            "signedAt": "2024-01-20T10:00:00Z",
                            "partnerSignedAt": "2024-01-18T15:30:00Z",
                            "managerSignedAt": "2024-01-20T10:00:00Z",
                            "createdAt": "2024-01-15T10:00:00Z",
                            "updatedAt": "2024-01-20T15:30:00Z",
                            "partnerId": 1,
                            "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                            "partnerEmail": "partner@example.com",
                            "partnerPhone": "0912345678",
                            "managerId": 1,
                            "managerName": "Trần Văn Manager",
                            "managerEmail": "manager@example.com"
                          },
                          {
                            "contractId": 2,
                            "contractNumber": "HD-2024-002",
                            "title": "HỢP ĐỒNG DỊCH VỤ BỔ SUNG",
                            "contractType": "service",
                            "startDate": "2024-03-01",
                            "endDate": "2024-09-01",
                            "commissionRate": 12.0,
                            "minimumRevenue": 50000000,
                            "status": "pending",
                            "isLocked": false,
                            "isActive": false,
                            "partnerSignatureUrl": null,
                            "managerSignature": null,
                            "signedAt": null,
                            "partnerSignedAt": null,
                            "managerSignedAt": null,
                            "createdAt": "2024-01-20T08:00:00Z",
                            "updatedAt": "2024-01-20T08:00:00Z",
                            "partnerId": 1,
                            "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                            "partnerEmail": "partner@example.com",
                            "partnerPhone": "0912345678",
                            "managerId": 1,
                            "managerName": "Trần Văn Manager",
                            "managerEmail": "manager@example.com"
                          }
                        ],
                        "pagination": {
                          "currentPage": 1,
                          "pageSize": 10,
                          "totalCount": 2,
                          "totalPages": 1,
                          "hasPrevious": false,
                          "hasNext": false
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
                      "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách hợp đồng.",
                      "detail": "Database connection error"
                    }
                    """
                        )
                    });
                }
            }
        }
    }
}
