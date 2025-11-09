using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Catalog
{
    public class Catalog_GetShowtimeSeats_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation op, OperationFilterContext ctx)
        {
            var c = ctx.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var a = ctx.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (c != "Catalog" || a != "GetShowtimeSeats")
                return;

            // ===== 200 =====
            var ok = op.Responses["200"].Content["application/json"];
            ok.Examples.Clear();
            ok.Examples.Add("Success", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "showtimeId": 1201,
                  "screenId": 30,
                  "seats": [
                    { "seatId": 101, "row": "A", "number": 1, "status": "AVAILABLE" },
                    { "seatId": 102, "row": "A", "number": 2, "status": "LOCKED" },
                    { "seatId": 103, "row": "A", "number": 3, "status": "SOLD" },
                    { "seatId": 201, "row": "B", "number": 1, "status": "AVAILABLE" }
                  ]
                }
                """
            )
            });

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

            // ===== 500 =====
            var se = op.Responses["500"].Content["application/json"];
            se.Examples.Clear();
            se.Examples.Add("Server Error", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Lỗi hệ thống khi lấy sơ đồ ghế."
                }
                """
            )
            });
        }
    }
}
