using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class UpdateMovieReviewExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // PUT /cinema/movies/{movieId}/reviews
            if (method == "PUT" && path?.Contains("/reviews") == true)
            {
                ApplyUpdateReviewExamples(operation);
            }
        }

        private void ApplyUpdateReviewExamples(OpenApiOperation operation)
        {
            // Request body example
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Update to 4 stars", new OpenApiExample
                    {
                        Summary = "Update review to 4 stars",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 4,
                                "comment": "Xem lại lần 2 thấy cũng ổn, nhưng không quá xuất sắc."
                            }
                            """
                        )
                    });

                    content.Examples.Add("Update to 5 stars", new OpenApiExample
                    {
                        Summary = "Update review to 5 stars",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 5,
                                "comment": "Xem lại thấy càng hay, nội dung sâu sắc!"
                            }
                            """
                        )
                    });

                    content.Examples.Add("Downgrade to 2 stars", new OpenApiExample
                    {
                        Summary = "Downgrade review to 2 stars",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 2,
                                "comment": "Sau khi suy nghĩ lại thì phim này không hay như tôi nghĩ."
                            }
                            """
                        )
                    });
                }
            }

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
                        Summary = "Review updated successfully",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Cập nhật review thành công",
                                "result": {
                                    "rating_id": 1001,
                                    "movie_id": 123,
                                    "user_id": 10,
                                    "rating_star": 4,
                                    "comment": "Xem lại lần 2 thấy cũng ổn, nhưng không quá xuất sắc.",
                                    "rating_at": "2025-11-16T11:00:00Z"
                                }
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid rating",
                """
                {
                    "message": "Lỗi xác thực dữ liệu",
                    "errors": {
                        "rating_star": {
                            "msg": "Số sao đánh giá phải từ 1 đến 5",
                            "path": "rating_star",
                            "location": "body"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Empty comment",
                """
                {
                    "message": "Lỗi xác thực dữ liệu",
                    "errors": {
                        "comment": {
                            "msg": "Bình luận không được để trống",
                            "path": "comment",
                            "location": "body"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Comment too long",
                """
                {
                    "message": "Lỗi xác thực dữ liệu",
                    "errors": {
                        "comment": {
                            "msg": "Bình luận không được vượt quá 1000 ký tự",
                            "path": "comment",
                            "location": "body"
                        }
                    }
                }
                """);

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
                        "movieId": {
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
                    "message": "Đã xảy ra lỗi hệ thống trong quá trình cập nhật review."
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

