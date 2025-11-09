using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetAllShowtimesExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetAllShowtimes")
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
                            "showtimes": [
                              {
                                "showtime_id": "1",
                                "movie_id": "1",
                                "screen_id": "1",
                                "cinema_id": "1",
                                "start_time": "2023-07-05T18:30:00.000Z",
                                "end_time": "2023-07-05T21:30:00.000Z",
                                "base_price": "100.00",
                                "format_type": "2d",
                                "available_seats": 120,
                                "status": "scheduled",
                                "movie": {
                                  "movie_id": 1,
                                  "title": "Avengers: Endgame",
                                  "description": "After the devastating events of Avengers: Infinity War...",
                                  "poster_url": "https://example.com/poster.jpg",
                                  "duration": 181,
                                  "genre": "Musical, Romance, Drama",
                                  "language": "English"
                                },
                                "cinema": {
                                  "cinema_id": 1,
                                  "name": "Lotte",
                                  "address": "123 Main Street",
                                  "city": "Hà Nội",
                                  "district": "Tây Hồ",
                                  "email": "galaxy@gmail.com"
                                },
                                "screen": {
                                  "screen_id": 1,
                                  "name": "Screen 1",
                                  "screen_type": "imax",
                                  "sound_system": "DTS:X",
                                  "description": "Phòng chiếu tiêu chuẩn với âm thanh Dolby Digital",
                                  "seat_rows": 10,
                                  "seat_columns": 10,
                                  "capacity": 150
                                }
                              }
                            ],
                            "total": 50,
                            "page": 1,
                            "limit": 10,
                            "total_pages": 5
                          }
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}