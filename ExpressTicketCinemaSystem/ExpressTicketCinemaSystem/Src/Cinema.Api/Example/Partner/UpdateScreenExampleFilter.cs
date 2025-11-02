using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class UpdateScreenExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UpdateScreen")
            {
                return;
            }

            // Parameters examples
            operation.Parameters ??= new List<OpenApiParameter>();

            var screenIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "screen_id");
            if (screenIdParam != null)
            {
                screenIdParam.Description = "Screen ID";
                screenIdParam.Examples = new Dictionary<string, OpenApiExample>
                {
                    ["Example"] = new OpenApiExample { Value = new OpenApiString("1") }
                };
            }

            // Request Body Example
            if (operation.RequestBody != null)
            {
                operation.RequestBody.Description = "Update screen request";
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Screen", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "screenName": "Phòng 1 - Standard (Updated)",
                          "description": "Phòng chiếu tiêu chuẩn đã được nâng cấp âm thanh",
                          "screenType": "standard",
                          "soundSystem": "Dolby Digital EX",
                          "capacity": 130,
                          "isActive": true
                        }
                        """
                        )
                    });
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
                          "message": "Cập nhật phòng thành công",
                          "result": {
                            "screenId": 1,
                            "cinemaId": 4,
                            "cinemaName": "Lotte Hòa Lạc",
                            "screenName": "Phòng 1 - Standard (Updated)",
                            "code": "LOTTE_HL_P1",
                            "description": "Phòng chiếu tiêu chuẩn đã được nâng cấp âm thanh",
                            "screenType": "standard",
                            "soundSystem": "Dolby Digital EX",
                            "capacity": 130,
                            "seatRows": 10,
                            "seatColumns": 12,
                            "isActive": true,
                            "hasSeatLayout": true,
                            "createdDate": "2024-01-15T08:00:00Z",
                            "updatedDate": "2024-01-25T15:45:00Z"
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
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "screenName": {
                              "msg": "Tên phòng không được vượt quá 255 ký tự",
                              "path": "screenName"
                            },
                            "isActive": {
                              "msg": "Không thể vô hiệu hóa phòng đã có layout ghế",
                              "path": "isActive"
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
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "access": {
                              "msg": "Tài khoản partner đã bị vô hiệu hóa",
                              "path": "authorization",
                              "location": "header"
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
                          "message": "Không tìm thấy phòng với ID này hoặc không thuộc quyền quản lý của bạn"
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
                          "message": "Đã xảy ra lỗi hệ thống khi cập nhật phòng."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}