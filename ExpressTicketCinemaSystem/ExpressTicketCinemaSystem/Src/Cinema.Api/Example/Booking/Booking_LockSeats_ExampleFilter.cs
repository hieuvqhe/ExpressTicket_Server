using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_LockSeats_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var (c, a) = (
                context.ApiDescription.ActionDescriptor.RouteValues["controller"],
                context.ApiDescription.ActionDescriptor.RouteValues["action"]
            );

            if (c != "BookingSessionsSeats" || a != "Lock")
                return;

            // ===== REQUEST =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Lock Seats", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "seatIds": [21, 22, 23]
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 200 =====
            if (operation.Responses.TryGetValue("200", out var resp200))
            {
                var content = resp200.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Success", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "bookingSessionId": "7dbcea0f-1e2b-4cff-a931-d9a0fe5ec4e1",
                      "showtimeId": 1201,
                      "lockedSeatIds": [21, 22, 23],
                      "lockedUntil": "2025-11-01T14:40:00Z",
                      "currentSeatIds": [10, 11, 21, 22, 23]
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 409 =====
            if (operation.Responses.TryGetValue("409", out var resp409))
            {
                var content = resp409.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Conflict", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Data conflict",
                      "errors": {
                        "seatIds": {
                          "msg": "Ghế đang bị giữ: 21",
                          "path": "seatIds"
                        }
                      }
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 400 =====
            if (operation.Responses.TryGetValue("400", out var resp400))
            {
                var content = resp400.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Validation Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Validation failed",
                      "errors": {
                        "seatIds": {
                          "msg": "Bạn chỉ có thể chọn tối đa 8 ghế mỗi lần đặt.",
                          "path": "seatIds"
                        }
                      }
                    }
                    """
                    )
                });
            }
        }
    }
}
