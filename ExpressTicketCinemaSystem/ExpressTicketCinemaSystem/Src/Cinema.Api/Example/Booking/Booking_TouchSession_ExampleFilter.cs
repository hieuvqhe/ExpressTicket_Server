using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_TouchSession_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controller != "BookingSessions" || action != "Touch")
                return;

            // ===== RESPONSE 200 =====
            if (operation.Responses.TryGetValue("200", out var ok))
            {
                var content = ok.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Success", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "bookingSessionId": "1ec8dcdf-dd59-4f2a-9f76-dde0d657ea20",
                      "expiresAt": "2025-11-01T14:55:00Z",
                      "lockedSeatsExtended": [12, 13, 14]
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 404 =====
            if (operation.Responses.TryGetValue("404", out var resp404))
            {
                resp404.Content["application/json"].Examples.Clear();
                resp404.Content["application/json"].Examples.Add("Not Found", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Không tìm thấy session"
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 400 =====
            if (operation.Responses.TryGetValue("400", out var resp400))
            {
                resp400.Content["application/json"].Examples.Clear();
                resp400.Content["application/json"].Examples.Add("Validation Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Validation failed",
                      "errors": {
                        "session": {
                          "msg": "Session đã hết hạn",
                          "path": "session"
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
