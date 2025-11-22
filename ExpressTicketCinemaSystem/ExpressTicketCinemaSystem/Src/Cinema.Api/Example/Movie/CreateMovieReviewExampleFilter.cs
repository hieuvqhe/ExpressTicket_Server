using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class CreateMovieReviewExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // POST /cinema/movies/{movieId}/reviews
            if (method == "POST" && path?.Contains("/reviews") == true)
            {
                ApplyCreateReviewExamples(operation);
            }
        }

        private void ApplyCreateReviewExamples(OpenApiOperation operation)
        {
            // Request body example
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Valid Review", new OpenApiExample
                    {
                        Summary = "Create review with 5 stars and images",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 5,
                                "comment": "Rất hay, âm nhạc đỉnh. Đáng xem!",
                                "image_urls": [
                                    "https://example.com/storage/review1.jpg",
                                    "https://example.com/storage/review2.jpg",
                                    "https://example.com/storage/review3.jpg"
                                ]
                            }
                            """
                        )
                    });

                    content.Examples.Add("Review without images", new OpenApiExample
                    {
                        Summary = "Create review without images",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 5,
                                "comment": "Rất hay, âm nhạc đỉnh. Đáng xem!"
                            }
                            """
                        )
                    });

                    content.Examples.Add("Minimum Rating", new OpenApiExample
                    {
                        Summary = "Create review with 1 star",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 1,
                                "comment": "Không hay lắm, cốt truyện nhàm chán.",
                                "image_urls": []
                            }
                            """
                        )
                    });

                    content.Examples.Add("Invalid - Too many images", new OpenApiExample
                    {
                        Summary = "❌ Invalid - Maximum 3 images allowed",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 5,
                                "comment": "Test với quá nhiều ảnh",
                                "image_urls": [
                                    "https://example.com/storage/review1.jpg",
                                    "https://example.com/storage/review2.jpg",
                                    "https://example.com/storage/review3.jpg",
                                    "https://example.com/storage/review4.jpg"
                                ]
                            }
                            """
                        )
                    });

                    content.Examples.Add("Invalid Rating (Will Fail)", new OpenApiExample
                    {
                        Summary = "❌ Invalid - rating_star must be 1-5",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 6,
                                "comment": "Rất hay, âm nhạc đỉnh. Đáng xem!"
                            }
                            """
                        )
                    });

                    content.Examples.Add("Invalid Rating Zero (Will Fail)", new OpenApiExample
                    {
                        Summary = "❌ Invalid - rating_star cannot be 0",
                        Value = new OpenApiString(
                            """
                            {
                                "rating_star": 0,
                                "comment": "Test với rating 0"
                            }
                            """
                        )
                    });
                }
            }

            // Response 201 Created
            if (operation.Responses.ContainsKey("201"))
            {
                var response = operation.Responses["201"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "Review created successfully",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Đã tạo review thành công",
                                "result": {
                                    "rating_id": 1001,
                                    "movie_id": 123,
                                    "user_id": 10,
                                    "user_name": "Nguyễn Huy Toàn",
                                    "user_avatar": "https://example.com/storage/avatars/user10.jpg",
                                    "rating_star": 5,
                                    "comment": "Rất hay, âm nhạc đỉnh. Đáng xem!",
                                    "rating_at": "2025-11-16T10:30:00Z",
                                    "image_urls": [
                                        "https://example.com/storage/review1.jpg",
                                        "https://example.com/storage/review2.jpg",
                                        "https://example.com/storage/review3.jpg"
                                    ]
                                }
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid rating (too high)",
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

            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid rating (too low)",
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
                        "Comment": {
                            "msg": "Bình luận không được vượt quá 1000 ký tự",
                            "path": "Comment",
                            "location": "body"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Too many images",
                """
                {
                    "message": "Lỗi xác thực dữ liệu",
                    "errors": {
                        "image_urls": {
                            "msg": "Tối đa 3 ảnh được phép",
                            "path": "image_urls",
                            "location": "body"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid image URL format",
                """
                {
                    "message": "Lỗi xác thực dữ liệu",
                    "errors": {
                        "image_urls": {
                            "msg": "URL ảnh không hợp lệ. Phải là URL ảnh (jpg, jpeg, png)",
                            "path": "image_urls",
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

            AddErrorResponseExamples(operation, "403", "Forbidden - User hasn't purchased ticket",
                """
                {
                    "message": "Bạn cần mua vé và thanh toán thành công để đánh giá phim này",
                    "errors": {
                        "permission": {
                            "msg": "Bạn cần mua vé và thanh toán thành công để đánh giá phim này",
                            "path": "movieId",
                            "location": "path"
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

            AddErrorResponseExamples(operation, "409", "Conflict - Already reviewed",
                """
                {
                    "message": "Bạn đã đánh giá phim này rồi",
                    "errors": {
                        "review": {
                            "msg": "Bạn đã đánh giá phim này rồi",
                            "path": "movieId",
                            "location": "path"
                        }
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "Đã xảy ra lỗi hệ thống trong quá trình tạo review."
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

