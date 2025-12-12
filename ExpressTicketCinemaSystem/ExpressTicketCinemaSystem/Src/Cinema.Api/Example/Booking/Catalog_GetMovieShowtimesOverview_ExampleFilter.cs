using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Catalog
{
    public class Catalog_GetMovieShowtimesOverview_ExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controller != "Catalog" || action != "GetMovieShowtimesOverview")
                return;

            // ===== RESPONSE 200 =====
            var ok = operation.Responses["200"].Content["application/json"];
            ok.Examples.Clear();
            ok.Examples.Add("Success", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "movieId": 501,
                  "movieName": "Avengers: Secret Wars",
                  "duration": 145,
                  "showtimes": [
                    {
                      "showtimeId": 1201,
                      "startTime": "2025-11-02T18:00:00Z",
                      "screenName": "Phòng 3",
                      "cinemaName": "Lotte Mỹ Đình"
                    },
                    {
                      "showtimeId": 1202,
                      "startTime": "2025-11-02T20:30:00Z",
                      "screenName": "Phòng 1",
                      "cinemaName": "Lotte Mỹ Đình"
                    }
                  ]
                }
                """
            )
            });

            // ===== RESPONSE 404 =====
            var notFound = operation.Responses["404"].Content["application/json"];
            notFound.Examples.Clear();
            notFound.Examples.Add("Not Found", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Không tìm thấy movie"
                }
                """
            )
            });

            // ===== RESPONSE 500 =====
            var server = operation.Responses["500"].Content["application/json"];
            server.Examples.Clear();
            server.Examples.Add("Server Error", new OpenApiExample
            {
                Value = new OpenApiString(
                """
                {
                  "message": "Lỗi hệ thống khi lấy thông tin suất chiếu."
                }
                """
            )
            });
        }
    }
}
