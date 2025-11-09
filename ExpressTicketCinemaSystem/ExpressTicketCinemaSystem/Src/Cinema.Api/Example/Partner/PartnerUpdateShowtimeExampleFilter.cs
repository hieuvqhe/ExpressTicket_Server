using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerUpdateShowtimeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            if (controller != "Partners" || action != "UpdateShowtime") return;

            // ===== Request (camelCase) =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update Showtime Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "movieId": 1,
                          "screenId": 1,
                          "cinemaId": 1,
                          "startTime": "2025-11-10T14:00:00Z",
                          "endTime":   "2025-11-10T16:00:00Z",
                          "basePrice": 150000,
                          "availableSeats": 100,
                          "formatType": "2D",
                          "status": "scheduled"
                        }
                        """
                        )
                    });
                }
            }

            // ===== 200 OK =====
            if (operation.Responses.ContainsKey("200"))
            {
                var content = operation.Responses["200"].Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Cập nhật showtime thành công",
                          "result": { "showtimeId": 1 }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 404 Not Found (chỉ not found) =====
            if (operation.Responses.ContainsKey("404"))
            {
                var content = operation.Responses["404"].Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Showtime Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        { "message": "Không tìm thấy suất chiếu với ID đã cho" }
                        """
                        )
                    });
                }
            }

            // ===== 400 Bad Request (Validation) =====
            if (operation.Responses.ContainsKey("400"))
            {
                var content = operation.Responses["400"].Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();

                    content.Examples.Add("Past Time", new OpenApiExample
                    {
                        Summary = "Thời gian bắt đầu trong quá khứ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "startTime": {
                              "msg": "Thời gian bắt đầu không thể trong quá khứ",
                              "path": "startTime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Invalid Time Range", new OpenApiExample
                    {
                        Summary = "endTime phải sau startTime",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "endTime": {
                              "msg": "Thời gian kết thúc phải sau thời gian bắt đầu",
                              "path": "endTime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Movie Not Found/Inactive", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "movieId": {
                              "msg": "Phim không tồn tại hoặc không hoạt động",
                              "path": "movieId",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Movie Not Released", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "movieId": {
                              "msg": "Phim chưa đến ngày công chiếu. Ngày công chiếu: 01/11/2025",
                              "path": "movieId",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Movie Ended", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "movieId": {
                              "msg": "Phim đã kết thúc thời gian công chiếu. Ngày kết thúc: 15/10/2025",
                              "path": "movieId",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Showtime > Movie +30min", new OpenApiExample
                    {
                        Summary = "Thời lượng suất chiếu vượt quá quy định",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "endTime": {
                              "msg": "Thời lượng suất chiếu không được vượt quá thời lượng phim quá 30 phút. Thời lượng phim: 120 phút, Thời lượng tối đa cho phép: 150 phút, Thời lượng hiện tại: 160 phút",
                              "path": "endTime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Available Seats Exceed Capacity", new OpenApiExample
                    {
                        Summary = "Ghế vượt sức chứa",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "availableSeats": {
                              "msg": "Số ghế có sẵn không được vượt quá sức chứa của phòng chiếu. Sức chứa tối đa: 100, Số ghế hiện tại: 120",
                              "path": "availableSeats",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Invalid Status", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "status": {
                              "msg": "Trạng thái không hợp lệ. Trạng thái hợp lệ: scheduled, finished, disabled",
                              "path": "status",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Cinema/Screen Access Denied", new OpenApiExample
                    {
                        Summary = "Không có quyền với rạp/phòng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "cinemaId": {
                              "msg": "Rạp chiếu không thuộc về partner của bạn hoặc không tồn tại",
                              "path": "cinemaId",
                              "location": "body"
                            },
                            "screenId": {
                              "msg": "Phòng chiếu không thuộc về rạp đã chọn hoặc không tồn tại",
                              "path": "screenId",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 409 Conflict (xung đột thực sự) =====
            if (operation.Responses.ContainsKey("409"))
            {
                var content = operation.Responses["409"].Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Overlapping Showtime", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xung đột dữ liệu",
                          "errors": {
                            "showtime": {
                              "msg": "Đã tồn tại suất chiếu khác trong khoảng thời gian này. Thời gian chiếu từ 10/11/2025 14:00 đến 10/11/2025 16:00",
                              "path": "showtime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 500 =====
            if (operation.Responses.ContainsKey("500"))
            {
                var content = operation.Responses["500"].Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        { "message": "Đã xảy ra lỗi hệ thống khi cập nhật suất chiếu." }
                        """
                        )
                    });
                }
            }
        }
    }
}