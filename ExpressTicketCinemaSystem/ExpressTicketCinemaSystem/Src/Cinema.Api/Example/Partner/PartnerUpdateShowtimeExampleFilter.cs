// Thêm file PartnerUpdateShowtimeExampleFilter.cs
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerUpdateShowtimeExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "UpdateShowtime")
            {
                return;
            }

            // Request Body Example
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
                          "movie_id": 1,
                          "screen_id": 1,
                          "cinema_id": 1,
                          "start_time": "2025-10-30T13:58:49.568Z",
                          "end_time": "2025-10-30T15:58:49.568Z",
                          "base_price": 150.00,
                          "available_seats": 100,
                          "format_type": "2D",
                          "status": "scheduled"
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
                          "message": "Cập nhật showtime thành công",
                          "result": {
                            "showtime_id": 1
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
                    content.Examples.Add("Showtime Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy suất chiếu với ID đã cho"
                        }
                        """
                        )
                    });

                    content.Examples.Add("Validation Error - Movie Not Released", new OpenApiExample
                    {
                        Summary = "Phim chưa đến ngày công chiếu",
                        Value = new OpenApiString(
    """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "movie_id": {
          "msg": "Phim chưa đến ngày công chiếu. Ngày công chiếu: 01/11/2025",
          "path": "movie_id",
          "location": "body"
        }
      }
    }
    """
    )
                    });

                    content.Examples.Add("Validation Error - Movie Ended", new OpenApiExample
                    {
                        Summary = "Phim đã kết thúc công chiếu",
                        Value = new OpenApiString(
                        """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "movie_id": {
          "msg": "Phim đã kết thúc thời gian công chiếu. Ngày kết thúc: 15/10/2025",
          "path": "movie_id",
          "location": "body"
        }
      }
    }
    """
                        )
                    });

                    content.Examples.Add("Validation Error - Showtime Duration Too Long", new OpenApiExample
                    {
                        Summary = "Thời lượng suất chiếu vượt quá thời lượng phim",
                        Value = new OpenApiString(
                        """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "end_time": {
          "msg": "Thời lượng suất chiếu không được vượt quá thời lượng phim quá 30 phút. Thời lượng phim: 120 phút, Thời lượng tối đa cho phép: 150 phút, Thời lượng hiện tại: 160 phút",
          "path": "end_time",
          "location": "body"
        }
      }
    }
    """
                        )
                    });

                    content.Examples.Add("Validation Error - Available Seats Exceed Capacity", new OpenApiExample
                    {
                        Summary = "Số ghế vượt quá sức chứa",
                        Value = new OpenApiString(
                        """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "available_seats": {
          "msg": "Số ghế có sẵn không được vượt quá sức chứa của phòng chiếu. Sức chứa tối đa: 100, Số ghế hiện tại: 120",
          "path": "available_seats",
          "location": "body"
        }
      }
    }
    """
                        )
                    });

                    // Trong phần Response 409 Conflict cho Delete, thêm example mới:
                    content.Examples.Add("Has Bookings", new OpenApiExample
                    {
                        Summary = "Suất chiếu đã có người đặt vé",
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

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Validation Error - Past Time", new OpenApiExample
                    {
                        Summary = "Thời gian trong quá khứ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "start_time": {
                              "msg": "Thời gian bắt đầu không thể trong quá khứ",
                              "path": "start_time",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    // Các examples validation khác tương tự như Create...
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
                    content.Examples.Add("Overlapping Showtime", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xung đột dữ liệu",
                          "errors": {
                            "showtime": {
                              "msg": "Đã tồn tại suất chiếu khác trong khoảng thời gian này. Thời gian chiếu từ 30/10/2025 14:00 đến 30/10/2025 16:00",
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
                          "message": "Đã xảy ra lỗi hệ thống khi cập nhật suất chiếu."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}