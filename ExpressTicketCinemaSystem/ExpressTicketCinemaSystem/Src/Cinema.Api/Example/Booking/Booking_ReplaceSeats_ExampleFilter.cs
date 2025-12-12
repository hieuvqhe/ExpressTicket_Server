using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_ReplaceSeats_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation op, OperationFilterContext ctx)
        {
            var c = ctx.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var a = ctx.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (c != "BookingSessionsSeats" || a != "Replace")
                return;

            // ===== REQUEST =====
            var req = op.RequestBody.Content["application/json"];
            req.Examples.Clear();
            req.Examples.Add("Replace Seats", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "seatIds": [30, 31, 32]
                }
                """
                )
            });

            // ===== 200 =====
            var resp200 = op.Responses["200"].Content["application/json"];
            resp200.Examples.Clear();
            resp200.Examples.Add("Success", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "bookingSessionId": "d40d7acc-a501-4133-a879-2de32b828c63",
                  "showtimeId": 1201,
                  "newSeatIds": [30, 31, 32],
                  "releasedSeatIds": [21, 22, 23]
                }
                """
                )
            });

            // ===== 409 =====
            var resp409 = op.Responses["409"].Content["application/json"];
            resp409.Examples.Clear();
            resp409.Examples.Add("Conflict", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Data conflict",
                  "errors": {
                    "seatIds": {
                      "msg": "Ghế đang bị giữ bởi session khác",
                      "path": "seatIds"
                    }
                  }
                }
                """
                )
            });

            // ===== 400 =====
            var resp400 = op.Responses["400"].Content["application/json"];
            resp400.Examples.Clear();
            resp400.Examples.Add("Validation Error", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Validation failed",
                  "errors": {
                    "seatIds": {
                      "msg": "Không thể để trống 1 ghế ở giữa.",
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
