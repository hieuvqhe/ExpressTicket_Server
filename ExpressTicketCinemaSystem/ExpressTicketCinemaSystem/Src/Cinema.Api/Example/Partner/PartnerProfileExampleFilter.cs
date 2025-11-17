using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetProfileExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetProfile")
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
                          "message": "Lấy thông tin partner thành công",
                          "result": {
                            "userId": 5,
                            "email": "cinemastar@example.com",
                            "phone": "0912345678",
                            "fullName": "Nguyễn Văn A (Đại diện)",
                            "avatarUrl": null, 
                            "partnerId": 5,
                            "partnerName": "Rạp Chiếu Phim Cinema Star",
                            "taxCode": "0123456789",
                            "address": "123 Nguyễn Huệ, Quận 1, TP.HCM",
                            "commissionRate": 10.5,
                            "businessRegistrationCertificateUrl": "https://example.com/certs/business.pdf",
                            "taxRegistrationCertificateUrl": "https://example.com/certs/tax.pdf",
                            "identityCardUrl": "https://example.com/certs/cccd.jpg",
                            "theaterPhotosUrls": [
                              "https://example.com/photos/theater1.jpg",
                              "https://example.com/photos/theater2.jpg"
                            ],
                            "additionalDocumentsUrls": [
                              "https://example.com/docs/doc1.jpg",
                              "https://example.com/docs/doc2.jpg"
                            ],
                            "status": "approved",
                            "createdAt": "2025-10-19T10:30:00Z",
                            "updatedAt": "2025-10-19T11:00:00Z"
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
                    content.Examples.Add("Not Approved", new OpenApiExample
                    {
                        Summary = "Partner chưa duyệt",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Tài khoản đối tác của bạn chưa được duyệt (trạng thái: pending).",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Invalid Token", new OpenApiExample
                    {
                        Summary = "Token không hợp lệ",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Xác thực thất bại",
                           "errors": {
                             "auth": {
                               "msg": "Token không hợp lệ hoặc không chứa ID người dùng.",
                               "path": "form",
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
                    content.Examples.Add("Partner Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy hồ sơ đối tác cho người dùng này."
                        }
                        """
                        )
                    });
                }
            }

            // Response 500
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin partner."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}