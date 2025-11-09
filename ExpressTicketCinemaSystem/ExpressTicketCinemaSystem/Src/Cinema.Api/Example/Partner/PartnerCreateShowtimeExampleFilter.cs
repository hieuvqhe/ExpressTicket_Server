using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerCreateShowtimeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "CreateShowtime")
                return;

            // ===== Request Body (camelCase) =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Create Showtime Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "movieId": 1,
                          "screenId": 1,
                          "cinemaId": 1,
                          "startTime": "2025-11-10T10:00:00Z",
                          "endTime":   "2025-11-10T12:30:00Z",
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
                          "message": "Tạo showtime thành công",
                          "result": {
                            "showtimeId": 1
                          }
                        }
                        """
                        )
                    });
                }
            }

            // ===== 400 Bad Request (Validation) =====
            if (operation.Responses.ContainsKey("400"))
            {
                var resp = operation.Responses["400"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();

                    content.Examples.Add("Past Time", new OpenApiExample
                    {
                        Summary = "Thời gian trong quá khứ",
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
                        Summary = "Khoảng thời gian không hợp lệ",
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

                    content.Examples.Add("Movie Not Found", new OpenApiExample
                    {
                        Summary = "Phim không tồn tại",
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

                    content.Examples.Add("Showtime Duration Too Long (vs movie)", new OpenApiExample
                    {
                        Summary = "Thời lượng suất chiếu vượt quá thời lượng phim +30'",
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
                        Summary = "Số ghế vượt quá sức chứa",
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
                        Summary = "Trạng thái không hợp lệ",
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

                    content.Examples.Add("Movie Not Released", new OpenApiExample
                    {
                        Summary = "Phim chưa đến ngày công chiếu",
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
                        Summary = "Phim đã kết thúc công chiếu",
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

                    // Truy cập rạp/phòng sai (ValidationException -> 400)
                    content.Examples.Add("Cinema/Screen Access Denied", new OpenApiExample
                    {
                        Summary = "Không có quyền truy cập rạp/phòng",
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

            // ===== 409 Conflict =====
            if (operation.Responses.ContainsKey("409"))
            {
                var resp = operation.Responses["409"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
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
                              "msg": "Đã tồn tại suất chiếu khác trong khoảng thời gian này. Thời gian chiếu từ 30/10/2025 10:00 đến 30/10/2025 12:30",
                              "path": "showtime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    content.Examples.Add("Has Bookings", new OpenApiExample
                    {
                        Summary = "Suất chiếu đã có người đặt vé (khi xóa)",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xung đột dữ liệu",
                          "errors": {
                            "showtime": {
                              "msg": "Không thể xóa suất chiếu vì đã có người đặt vé. Chỉ có thể xóa các suất chiếu chưa có người đặt.",
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
                var resp = operation.Responses["500"];
                var content = resp.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi tạo suất chiếu."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}
