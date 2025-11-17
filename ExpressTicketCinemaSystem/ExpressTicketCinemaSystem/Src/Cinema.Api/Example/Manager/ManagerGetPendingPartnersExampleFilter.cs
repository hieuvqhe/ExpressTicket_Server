using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerGetPendingPartnersExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "GetPendingPartners")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            // Page parameter
            var pageParam = operation.Parameters.FirstOrDefault(p => p.Name == "page");
            if (pageParam != null)
            {
                pageParam.Description = "Page number (default: 1)";
                pageParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(1)
                    }
                };
            }

            // Limit parameter
            var limitParam = operation.Parameters.FirstOrDefault(p => p.Name == "limit");
            if (limitParam != null)
            {
                limitParam.Description = "Number of items per page (default: 10, max: 100)";
                limitParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiInteger(10)
                    }
                };
            }

            // Search parameter
            var searchParam = operation.Parameters.FirstOrDefault(p => p.Name == "search");
            if (searchParam != null)
            {
                searchParam.Description = "Search term for partner name, email, phone, or tax code";
                searchParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiString("ABC Cinema")
                    }
                };
            }

            // SortBy parameter
            var sortByParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortBy");
            if (sortByParam != null)
            {
                sortByParam.Description = "Field to sort by (partner_name, email, phone, tax_code, created_at, updated_at)";
                sortByParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiString("created_at")
                    }
                };
            }

            // SortOrder parameter
            var sortOrderParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortOrder");
            if (sortOrderParam != null)
            {
                sortOrderParam.Description = "Sort order (asc, desc)";
                sortOrderParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample
                    {
                        Value = new OpenApiString("desc")
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
                          "message": "Lấy danh sách partner chờ duyệt thành công",
                          "result": {
                            "partners": [
                              {
                                "partnerId": 1,
                                "partnerName": "CÔNG TY TNHH RẠP PHIM ABC",
                                "taxCode": "0123456789",
                                "address": "123 Đường XYZ, Quận 1, TP.HCM",
                                "email": "cinema.abc@example.com",
                                "phone": "0912345678",
                                "commissionRate": 15.5,
                                "status": "pending",
                                "createdAt": "2024-01-15T08:00:00Z",
                                "updatedAt": "2024-01-15T08:00:00Z",
                                "userId": 101,
                                "fullname": "Nguyễn Văn A",
                                "userEmail": "nguyenvana@example.com",
                                "userPhone": "0912345678",
                                "businessRegistrationCertificateUrl": "https://example.com/docs/business-registration.jpg",
                                "taxRegistrationCertificateUrl": "https://example.com/docs/tax-registration.jpg",
                                "identityCardUrl": "https://example.com/docs/id-card.jpg",
                                "theaterPhotosUrl": "https://example.com/docs/theater-photos.jpg",
                                "additionalDocumentsUrl": "https://example.com/docs/additional-docs.jpg"
                              },
                              {
                                "partnerId": 2,
                                "partnerName": "CÔNG TY TNHH RẠP PHIM XYZ",
                                "taxCode": "9876543210",
                                "address": "456 Đường ABC, Quận 2, TP.HCM",
                                "email": "cinema.xyz@example.com",
                                "phone": "0987654321",
                                "commissionRate": 12.0,
                                "status": "pending",
                                "createdAt": "2024-01-16T09:00:00Z",
                                "updatedAt": "2024-01-16T09:00:00Z",
                                "userId": 102,
                                "fullname": "Trần Thị B",
                                "userEmail": "tranthib@example.com",
                                "userPhone": "0987654321",
                                "businessRegistrationCertificateUrl": "https://example.com/docs/business-registration-2.jpg",
                                "taxRegistrationCertificateUrl": "https://example.com/docs/tax-registration-2.jpg",
                                "identityCardUrl": "https://example.com/docs/id-card-2.jpg",
                                "theaterPhotosUrl": "https://example.com/docs/theater-photos-2.jpg",
                                "additionalDocumentsUrl": "https://example.com/docs/additional-docs-2.jpg"
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 15,
                              "totalPages": 2
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách partner chờ duyệt."
                        }
                        """
                        )
                    });
                }
            }

            // Thêm summary và description cho operation
            operation.Summary = "Get list of pending partners for approval";
            operation.Description = "Retrieve paginated list of partners with 'pending' status for manager review and approval process.";
        }
    }
}