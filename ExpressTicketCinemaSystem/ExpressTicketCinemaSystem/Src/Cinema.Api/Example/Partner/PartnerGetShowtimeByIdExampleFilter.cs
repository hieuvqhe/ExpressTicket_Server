// Thêm file PartnerGetShowtimeByIdExampleFilter.cs
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetShowtimeByIdExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetShowtimeById")
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
                          "message": "Get showtime thành công",
                          "result": {
                            "showtime_id": 1,
                            "movie_id": 1,
                            "screen_id": 1,
                            "cinema_id": 1,
                            "start_time": "2025-07-05T18:30:00.000Z",
                            "end_time": "2025-07-05T21:30:00.000Z",
                            "base_price": 100.00,
                            "format_type": "2D",
                            "available_seats": 120,
                            "status": "scheduled",
                            "movie": {
                              "movie_id": 1,
                              "title": "Avengers: Endgame",
                              "description": "After the devastating events of Avengers: Infinity War...",
                              "poster_url": "https://example.com/poster.jpg",
                              "duration": 181,
                              "genre": "Action, Adventure, Drama",
                              "language": "English"
                            },
                            "cinema": {
                              "cinema_id": 1,
                              "name": "Lotte Cinema",
                              "address": "123 Main Street",
                              "city": "Hà Nội",
                              "district": "Tây Hồ",
                              "email": "lotte@gmail.com"
                            },
                            "screen": {
                              "screen_id": 1,
                              "name": "Screen 1",
                              "screen_type": "IMAX",
                              "sound_system": "DTS:X",
                              "description": "Phòng chiếu tiêu chuẩn với âm thanh Dolby Digital",
                              "seat_rows": 10,
                              "seat_columns": 10,
                              "capacity": 150
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin suất chiếu."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}