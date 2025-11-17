using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class DeleteMovieReviewExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // DELETE /cinema/movies/{movieId}/reviews
            if (method == "DELETE" && path?.Contains("/reviews") == true)
            {
                ApplyDeleteReviewExamples(operation);
            }
        }

        private void ApplyDeleteReviewExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "Review deleted successfully",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Đã xoá review của bạn cho phim này",
                                "result": {}
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized - No token",
                """
                {
                    "message": "Yêu cầu cần được xác thực. Vui lòng cung cấp token hợp lệ.",
                    "errors": {
                        "auth": {
                            "msg": "Yêu cầu cần được xác thực. Vui lòng cung cấp token hợp lệ.",
                            "path": "header",
                            "location": "Authorization"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found - Movie not found",
                """
                {
                    "message": "Không tìm thấy phim",
                    "errors": {
                        "review": {
                            "msg": "Không tìm thấy phim",
                            "path": "movieId",
                            "location": "path"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found - Review not found",
                """
                {
                    "message": "Bạn chưa review phim này",
                    "errors": {
                        "review": {
                            "msg": "Bạn chưa review phim này",
                            "path": "movieId",
                            "location": "path"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "Đã xảy ra lỗi hệ thống trong quá trình xóa review."
                }
                """);
        }

        private void AddErrorResponseExamples(OpenApiOperation operation, string statusCode, string summary, string exampleJson)
        {
            if (operation.Responses.ContainsKey(statusCode))
            {
                var response = operation.Responses[statusCode];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    if (!content.Examples.ContainsKey($"{statusCode} - {summary}"))
                    {
                        content.Examples.Add($"{statusCode} - {summary}", new OpenApiExample
                        {
                            Summary = summary,
                            Value = new OpenApiString(exampleJson)
                        });
                    }
                }
            }
        }
    }
}


