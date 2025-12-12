using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Booking
{
    public class Booking_CreateSession_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controller != "BookingSessions" || action != "Create")
                return;

            // ===== REQUEST =====
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Create Session", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "showtimeId": 12345
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
                      "bookingSessionId": "96f0e3af-6cd0-43e0-884d-59fb46a1b321",
                      "showtimeId": 12345,
                      "state": "DRAFT",
                      "expiresAt": "2025-11-01T14:35:00Z",
                      "items": {
                        "seats": [],
                        "combos": []
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
                        "showtimeId": {
                          "msg": "showtimeId là bắt buộc",
                          "path": "showtimeId",
                          "location": "body"
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
                var content = resp404.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Not Found", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Showtime không tồn tại"
                    }
                    """
                    )
                });
            }

            // ===== RESPONSE 500 =====
            if (operation.Responses.TryGetValue("500", out var resp500))
            {
                var content = resp500.Content["application/json"];
                content.Examples.Clear();

                content.Examples.Add("Server Error", new OpenApiExample
                {
                    Value = new OpenApiString(
                    """
                    {
                      "message": "Đã xảy ra lỗi hệ thống khi tạo session."
                    }
                    """
                    )
                });
            }
        }
    }
}
