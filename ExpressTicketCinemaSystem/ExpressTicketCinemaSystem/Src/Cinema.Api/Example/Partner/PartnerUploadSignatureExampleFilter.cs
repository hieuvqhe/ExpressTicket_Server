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
                          "signedContractPdfUrl": "https://assets/docs/signed-contract-123.pdf",
                          "notes": "Đã ký hợp đồng và upload PDF hợp đồng đã ký"
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
                          "message": "Upload PDF hợp đồng đã ký thành công",
                          "result": {
                            "contractId": 1,
                            "contractNumber": "HD-2024-001",
                            "title": "HỢP ĐỒNG HỢP TÁC KINH DOANH",
                            "status": "pending",
                            "pdfUrl": "https://assets/docs/signed-contract-123.pdf",
                            "partnerSignatureUrl": "https://assets/docs/signed-contract-123.pdf",
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
                            "signedContractPdfUrl": {
                              "msg": "URL PDF hợp đồng đã ký là bắt buộc",
                              "path": "signedContractPdfUrl",
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
                              "msg": "Bạn không có quyền upload signature cho hợp đồng này",
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
                        Summary = "Không tìm thấy",
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
                          "message": "Đã xảy ra lỗi hệ thống khi upload PDF hợp đồng đã ký."
                        }
                        """
                        )
                    });
                }
            }

            operation.Summary = "Upload signed PDF contract";
            operation.Description = "Partner uploads signed PDF contract. This replaces the original PDF sent by manager, so manager will view the signed version when reviewing the contract.";
        }
    }
}