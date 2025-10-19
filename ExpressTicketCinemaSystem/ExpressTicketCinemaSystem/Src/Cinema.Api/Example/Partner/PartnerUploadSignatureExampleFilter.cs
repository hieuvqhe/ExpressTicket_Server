using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerUploadSignatureExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UploadSignature")
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
                          "signatureImageUrl": "https://example.com/signatures/partner-signature-123.jpg",
                          "notes": "Đã ký hợp đồng và upload biên bản ký"
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
                          "message": "Upload ảnh biên bản ký thành công",
                          "result": {
                            "contractId": 1,
                            "contractNumber": "HD-2024-001",
                            "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                            "status": "pending",
                            "partnerSignatureUrl": "https://example.com/signatures/partner-signature-123.jpg",
                            "partnerSignedAt": "2024-01-18T15:30:00Z",
                            "updatedAt": "2024-01-18T15:30:00Z"
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
                            "signatureImageUrl": {
                              "msg": "URL ảnh biên bản ký là bắt buộc",
                              "path": "signatureImageUrl",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}