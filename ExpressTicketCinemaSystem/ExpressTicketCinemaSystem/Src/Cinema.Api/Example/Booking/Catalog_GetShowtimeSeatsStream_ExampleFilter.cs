using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Catalog
{
    public class Catalog_GetShowtimeSeatsStream_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation op, OperationFilterContext ctx)
        {
            var c = ctx.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var a = ctx.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (c != "Catalog" || a != "StreamShowtimeSeats")
                return;

            if (op.Responses.TryGetValue("200", out var r200))
            {
                var content = r200.Content["text/event-stream"];
                content.Examples.Clear();

                content.Examples.Add("SSE Events", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    event: snapshot
                    data: {
                      "showtimeId": 1201,
                      "seats": [
                        { "seatId": 101, "status": "AVAILABLE" },
                        { "seatId": 102, "status": "LOCKED" }
                      ]
                    }

                    event: seat_locked
                    data: {
                      "seatId": 102,
                      "lockedUntil": "2025-11-01T14:45:00Z"
                    }

                    event: seat_released
                    data: {
                      "seatId": 102
                    }

                    event: seat_sold
                    data: {
                      "seatId": 103
                    }

                    event: heartbeat
                    data: { "time": "2025-11-01T14:40:00Z" }
                    """
                )
                });
            }

            // ===== 404 =====
            var nf = op.Responses["404"].Content["application/json"];
            nf.Examples.Clear();
            nf.Examples.Add("Not Found", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Không tìm thấy showtime"
                }
                """
            )
            });
        }
    }
}
