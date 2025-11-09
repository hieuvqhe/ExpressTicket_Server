using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_DeleteSession_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controller != "BookingSessions" || action != "Delete")
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
                      "bookingSessionId": "2c77ffa3-2b7f-4f25-a95d-1f675f5511bd",
                      "state": "CANCELED",
                      "releasedSeatIds": [10, 11, 12]
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
                          "msg": "Session không còn trạng thái DRAFT",
                          "path": "session"
                        }
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
        }
    }
}
