using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerGetAllVouchersExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "ManagerVoucher" || actionName != "GetAllVouchers")
            {
                return;
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
                          "message": "Lấy danh sách voucher thành công",
                          "result": {
                            "vouchers": [
                              {
                                "voucherId": 1,
                                "voucherCode": "SUMMER2025",
                                "discountType": "percent",
                                "discountVal": 15.0,
                                "validFrom": "2025-06-01",
                                "validTo": "2025-08-31",
                                "usageLimit": 1000,
                                "usedCount": 0,
                                "isActive": true,
                                "isRestricted": false,
                                "createdAt": "2024-01-15T10:00:00Z",
                                "managerName": "Trần Văn B"
                              },
                              {
                                "voucherId": 2,
                                "voucherCode": "WINTER2024",
                                "discountType": "fixed",
                                "discountVal": 30000.0,
                                "validFrom": "2024-12-01",
                                "validTo": "2024-12-31",
                                "usageLimit": 500,
                                "usedCount": 45,
                                "isActive": true,
                                "isRestricted": true,
                                "createdAt": "2024-01-10T08:30:00Z",
                                "managerName": "Trần Văn B"
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 2,
                              "totalPages": 1
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách voucher"
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}