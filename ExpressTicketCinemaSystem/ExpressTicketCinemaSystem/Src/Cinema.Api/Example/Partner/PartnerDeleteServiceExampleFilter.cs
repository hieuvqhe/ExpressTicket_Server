using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerDeleteServiceExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            if (controllerName != "Partners" || actionName != "DeleteService") return;

            // ===== 200 OK =====
            if (operation.Responses.ContainsKey("200"))
            {
                var resp = operation.Responses["200"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xóa combo thành công",
                          "result": {
                            "serviceId": 101,
                            "message": "Xóa combo thành công",
                            "isAvailable": false,
                            "updatedAt": "2025-11-05T03:10:00Z"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 401 =====
            if (operation.Responses.ContainsKey("401"))
            {
                var resp = operation.Responses["401"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Unauthorized", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Xác thực thất bại",
                      "errors": {}
                    }
                    """
                    )
                });
            }

            // ===== 404 =====
            if (operation.Responses.ContainsKey("404"))
            {
                var resp = operation.Responses["404"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Not Found", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Không tìm thấy combo với ID này hoặc không thuộc quyền quản lý của bạn"
                    }
                    """
                    )
                });
            }

            // ===== 500 =====
            if (operation.Responses.ContainsKey("500"))
            {
                var resp = operation.Responses["500"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                content?.Examples.Clear();
                content?.Examples.Add("Server Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Đã xảy ra lỗi hệ thống khi xóa combo."
                    }
                    """
                    )
                });
            }
        }
    }
}
