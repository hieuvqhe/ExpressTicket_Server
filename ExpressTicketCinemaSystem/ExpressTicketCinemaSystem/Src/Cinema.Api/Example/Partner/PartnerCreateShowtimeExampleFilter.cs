using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
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
                    content.Examples.Add("Create Showtime Request", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "movie_id": 1,
                          "screen_id": 1,
                          "cinema_id": 1,
                          "start_time": "2025-10-30T10:00:00.000Z",
                          "end_time": "2025-10-30T12:30:00.000Z",
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
                          "message": "Tạo showtime thành công",
                          "result": {
                            "showtime_id": 1
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
                    content.Examples.Add("Validation Error - Invalid Time Range", new OpenApiExample
                    {
                        Summary = "Khoảng thời gian không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "end_time": {
                              "msg": "Thời gian kết thúc phải sau thời gian bắt đầu",
                              "path": "end_time",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Validation Error - Movie Not Found", new OpenApiExample
                    {
                        Summary = "Phim không tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "movie_id": {
                              "msg": "Phim không tồn tại hoặc không hoạt động",
                              "path": "movie_id",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    // Thêm vào phần Response 400 Bad Request trong file ShowtimeExample.cs
                    content.Examples.Add("Validation Error - Duration Too Long", new OpenApiExample
                    {
                        Summary = "Thời lượng suất chiếu quá dài",
                        Value = new OpenApiString(
                        """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "end_time": {
          "msg": "Thời lượng suất chiếu không được vượt quá 4 tiếng. Thời lượng hiện tại: 4.5 giờ",
          "path": "end_time",
          "location": "body"
        }
      }
    }
    """
                        )
                    });
                    content.Examples.Add("Validation Error - Duration Too Short", new OpenApiExample
                    {
                        Summary = "Thời lượng suất chiếu quá ngắn",
                        Value = new OpenApiString(
                        """
    {
      "message": "Lỗi xác thực dữ liệu",
      "errors": {
        "end_time": {
          "msg": "Thời lượng suất chiếu phải ít nhất 30 phút. Thời lượng hiện tại: 20 phút",
          "path": "end_time",
          "location": "body"
        }
      }
    }
    """
                        )
                    });

                    content.Examples.Add("Validation Error - Invalid Status", new OpenApiExample
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
                              "msg": "Đã tồn tại suất chiếu khác trong khoảng thời gian này. Thời gian chiếu từ 30/10/2025 10:00 đến 30/10/2025 12:30",
                              "path": "showtime",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                    content.Examples.Add("Cinema/Screen Access Denied", new OpenApiExample
                    {
                        Summary = "Không có quyền truy cập rạp/phòng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xung đột dữ liệu",
                          "errors": {
                            "cinema_id": {
                              "msg": "Rạp chiếu không thuộc về partner của bạn hoặc không tồn tại",
                              "path": "cinema_id",
                              "location": "body"
                            },
                            "screen_id": {
                              "msg": "Phòng chiếu không thuộc về rạp đã chọn hoặc không tồn tại",
                              "path": "screen_id",
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