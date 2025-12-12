using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_GetSession_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controller != "BookingSessions" || action != "Get")
                return;

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
                      "bookingSessionId": "96f0e3af-6cd0-43e0-884d-59fb46a1b321",
                      "showtimeId": 1201,
                      "state": "DRAFT",
                      "expiresAt": "2025-11-01T15:10:00Z",
                      "items": {
                        "seats": [15, 16],
                        "combos": []
                      }
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

            // ===== RESPONSE 500 =====
            if (operation.Responses.TryGetValue("500", out var resp500))
            {
                resp500.Content["application/json"].Examples.Clear();
                resp500.Content["application/json"].Examples.Add("Server Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Đã xảy ra lỗi hệ thống."
                    }
                    """
                    )
                });
            }
        }
    }
}
