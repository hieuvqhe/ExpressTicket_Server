using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerRegisterExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "Register")
            {
                return;
            }

            // ==================== REQUEST BODY EXAMPLE ====================
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "email": "partner@example.com",
                          "password": "Partner123!",
                          "confirmPassword": "Partner123!",
                          "fullName": "Nguyễn Văn A",
                          "phone": "0912345678",
                          "partnerName": "Công ty TNHH Rạp Chiếu Phim ABC",
                          "taxCode": "0123456789",
                          "address": "123 Đường ABC, Quận 1, TP.HCM",
                          "commissionRate": 15.5,
                          "businessRegistrationCertificateUrl": "https://example.com/business.jpg",
                          "taxRegistrationCertificateUrl": "https://example.com/tax.pdf",
                          "identityCardUrl": "https://example.com/idcard.jpg",
                          "theaterPhotosUrls": [
                            "https://example.com/theater1.jpg",
                            "https://example.com/theater2.jpg"
                          ],
                          "additionalDocumentsUrls": [
                            "https://example.com/doc1.jpg",
                            "https://example.com/doc2.jpg"
                          ]
                        }
                        """
                        )
                    }
                }
            };

            // ==================== RESPONSE EXAMPLES ====================

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
                          "message": "Đăng ký đối tác thành công. Hồ sơ của bạn đang chờ xét duyệt.",
                          "result": {
                            "partnerId": 5, 
                            "status": "pending",
                            "createdAt": "2025-10-19T10:30:00Z" 
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
                    content.Examples.Add("Validation Error - Multiple", new OpenApiExample
                    {
                        Summary = "Nhiều lỗi validation",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "email": {
                              "msg": "Định dạng email không hợp lệ",
                              "path": "email",
                              "location": "body"
                            },
                            "password_digit": {
                              "msg": "Mật khẩu phải chứa ít nhất một chữ số",
                              "path": "password",
                              "location": "body"
                            },
                             "taxCode": {
                              "msg": "Mã số thuế phải có 10 hoặc 13 chữ số",
                              "path": "taxCode",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Validation Error - Required", new OpenApiExample
                    {
                        Summary = "Thiếu trường bắt buộc",
                        Value = new OpenApiString(
                       """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "partnerName": {
                              "msg": "Tên doanh nghiệp là bắt buộc",
                              "path": "partnerName",
                              "location": "body"
                            },
                            "identityCardUrl": {
                              "msg": "Ảnh CMND/CCCD là bắt buộc",
                              "path": "identityCardUrl",
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
                    content.Examples.Add("Email Exists", new OpenApiExample
                    {
                        Summary = "Email đã tồn tại",
                        Value = new OpenApiString(
                         """
                         {
                           "message": "Dữ liệu bị xung đột",
                           "errors": {
                             "email": {
                               "msg": "Email đã tồn tại trong hệ thống",
                               "path": "email",
                               "location": "body"
                             }
                           }
                         }
                         """
                         )
                    });
                    content.Examples.Add("Tax Code Exists", new OpenApiExample
                    {
                        Summary = "Mã số thuế đã tồn tại",
                        Value = new OpenApiString(
                        """
                         {
                           "message": "Dữ liệu bị xung đột",
                           "errors": {
                             "taxCode": {
                               "msg": "Mã số thuế đã được đăng ký",
                               "path": "taxCode",
                               "location": "body"
                             }
                           }
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
                          "message": "Đã xảy ra lỗi hệ thống trong quá trình đăng ký đối tác."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}