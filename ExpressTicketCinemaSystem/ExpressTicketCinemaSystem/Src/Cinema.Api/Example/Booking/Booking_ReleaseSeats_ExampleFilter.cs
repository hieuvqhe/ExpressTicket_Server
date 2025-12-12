using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_ReleaseSeats_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var (c, a) = (
                context.ApiDescription.ActionDescriptor.RouteValues["controller"],
                context.ApiDescription.ActionDescriptor.RouteValues["action"]
            );

            if (c != "BookingSessionsSeats" || a != "Release")
                return;

            // ===== REQUEST =====
            var req = operation.RequestBody.Content["application/json"];
            req.Examples.Clear();
            req.Examples.Add("Release Seats", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "seatIds": [10, 11]
                }
                """
                )
            });

            // ===== RESPONSE 200 =====
            var resp200 = operation.Responses["200"].Content["application/json"];
            resp200.Examples.Clear();
            resp200.Examples.Add("Success", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "bookingSessionId": "9c0c2f0f-aa1e-4a42-88ad-aca9bd65f3d0",
                  "showtimeId": 1201,
                  "releasedSeatIds": [10, 11],
                  "currentSeatIds": [21, 22, 23]
                }
                """
                )
            });

            // ===== RESPONSE 400 =====
            var resp400 = operation.Responses["400"].Content["application/json"];
            resp400.Examples.Clear();
            resp400.Examples.Add("Validation Error", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Validation failed",
                  "errors": {
                    "seatIds": {
                      "msg": "Danh sách ghế không được rỗng",
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
