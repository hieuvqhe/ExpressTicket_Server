using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerPatchProfileExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UpdateProfile")
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
                          "fullName": "Nguyễn Văn A (Đại diện - Đã cập nhật)",
                          "phone": "0912345999",
                          "partnerName": "Rạp Chiếu Phim Cinema Star Premium",
                          "taxCode": "0123456789",
                          "address": "456 Lê Lợi, Quận 1, TP.HCM",
                          "commissionRate": 12.5,
                          "businessRegistrationCertificateUrl": "https://example.com/certs/business-updated.pdf",
                          "taxRegistrationCertificateUrl": "https://example.com/certs/tax-updated.pdf",
                          "identityCardUrl": "https://example.com/certs/cccd-updated.jpg",
                          "theaterPhotosUrls": [
                            "https://example.com/photos/theater1-updated.jpg",
                            "https://example.com/photos/theater2-updated.jpg",
                            "https://example.com/photos/theater3-new.jpg"
                          ],
                          "additionalDocumentsUrls": [
                            "https://example.com/docs/doc1-updated.jpg",
                            "https://example.com/docs/doc2-new.jpg"
                          ]
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
                          "message": "Cập nhật thông tin partner thành công",
                          "result": {
                            "userId": 5,
                            "email": "cinemastar@example.com",
                            "phone": "0912345999",
                            "fullName": "Nguyễn Văn A (Đại diện - Đã cập nhật)",
                            "avatarUrl": null, 
                            "partnerId": 5,
                            "partnerName": "Rạp Chiếu Phim Cinema Star Premium",
                            "taxCode": "0123456789",
                            "address": "456 Lê Lợi, Quận 1, TP.HCM",
                            "commissionRate": 12.5,
                            "businessRegistrationCertificateUrl": "https://example.com/certs/business-updated.pdf",
                            "taxRegistrationCertificateUrl": "https://example.com/certs/tax-updated.pdf",
                            "identityCardUrl": "https://example.com/certs/cccd-updated.jpg",
                            "theaterPhotosUrls": [
                              "https://example.com/photos/theater1-updated.jpg",
                              "https://example.com/photos/theater2-updated.jpg",
                              "https://example.com/photos/theater3-new.jpg"
                            ],
                            "additionalDocumentsUrls": [
                              "https://example.com/docs/doc1-updated.jpg",
                              "https://example.com/docs/doc2-new.jpg"
                            ],
                            "status": "approved",
                            "createdAt": "2025-10-19T10:30:00Z",
                            "updatedAt": "2025-10-20T09:15:00Z"
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
                            "phone": {
                              "msg": "Định dạng số điện thoại không hợp lệ",
                              "path": "phone",
                              "location": "body"
                            },
                            "commissionRate": {
                              "msg": "Tỷ lệ hoa hồng không thể vượt quá 50%",
                              "path": "commissionRate",
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

            // Response 409 Conflict
            if (operation.Responses.ContainsKey("409"))
            {
                var response = operation.Responses["409"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Business Info Conflict", new OpenApiExample
                    {
                        Summary = "Thông tin kinh doanh trùng lặp",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "partnerName": {
                              "msg": "Tên doanh nghiệp đã tồn tại",
                              "path": "partnerName",
                              "location": "body"
                            },
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
                          "message": "Đã xảy ra lỗi hệ thống khi cập nhật thông tin partner."
                        }
                        """
                        )
                    });
                }
            }

            operation.Summary = "Update partner profile";
            operation.Description = "Update partner business information and contact details. Only approved partners can update their profile.";
        }
    }
}