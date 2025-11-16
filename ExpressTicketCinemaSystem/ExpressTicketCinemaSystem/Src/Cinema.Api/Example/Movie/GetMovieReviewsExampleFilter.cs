using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class GetMovieReviewsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // GET /cinema/movies/{movieId}/reviews
            if (method == "GET" && path?.Contains("{movieId}/reviews") == true)
            {
                // Xóa examples mặc định
                operation.Responses.Clear();

                // ===== SUCCESS RESPONSE (200 OK) =====
                operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "Lấy danh sách review thành công",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Success_NewestSort"] = new OpenApiExample
                                {
                                    Summary = "Success - Sorted by newest",
                                    Description = "Danh sách review của phim, sắp xếp theo mới nhất (mặc định)",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy danh sách review thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(3),
                                            ["page"] = new OpenApiInteger(1),
                                            ["limit"] = new OpenApiInteger(10),
                                            ["total_reviews"] = new OpenApiInteger(2),
                                            ["average_rating"] = new OpenApiDouble(8.5),
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["rating_id"] = new OpenApiInteger(30),
                                                    ["user_id"] = new OpenApiInteger(12),
                                                    ["user_name"] = new OpenApiString("Quang Ngoc"),
                                                    ["rating_star"] = new OpenApiInteger(2),
                                                    ["comment"] = new OpenApiString("Plot hơi rối và khó hiểu, mình không thích nhiều tầng nghĩa kiểu này. 2 sao."),
                                                    ["rating_at"] = new OpenApiString("2025-09-27T00:00:00")
                                                },
                                                new OpenApiObject
                                                {
                                                    ["rating_id"] = new OpenApiInteger(29),
                                                    ["user_id"] = new OpenApiInteger(11),
                                                    ["user_name"] = new OpenApiString("string"),
                                                    ["rating_star"] = new OpenApiInteger(5),
                                                    ["comment"] = new OpenApiString("Ý tưởng độc đáo, hình ảnh và âm nhạc rất ấn tượng. 5 sao."),
                                                    ["rating_at"] = new OpenApiString("2025-09-24T00:00:00")
                                                }
                                            }
                                        }
                                    }
                                },
                                ["Success_HighestSort"] = new OpenApiExample
                                {
                                    Summary = "Success - Sorted by highest rating",
                                    Description = "Danh sách review sắp xếp theo rating cao nhất",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy danh sách review thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(3),
                                            ["page"] = new OpenApiInteger(1),
                                            ["limit"] = new OpenApiInteger(5),
                                            ["total_reviews"] = new OpenApiInteger(2),
                                            ["average_rating"] = new OpenApiDouble(8.5),
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["rating_id"] = new OpenApiInteger(29),
                                                    ["user_id"] = new OpenApiInteger(11),
                                                    ["user_name"] = new OpenApiString("string"),
                                                    ["rating_star"] = new OpenApiInteger(5),
                                                    ["comment"] = new OpenApiString("Ý tưởng độc đáo, hình ảnh và âm nhạc rất ấn tượng. 5 sao."),
                                                    ["rating_at"] = new OpenApiString("2025-09-24T00:00:00")
                                                },
                                                new OpenApiObject
                                                {
                                                    ["rating_id"] = new OpenApiInteger(30),
                                                    ["user_id"] = new OpenApiInteger(12),
                                                    ["user_name"] = new OpenApiString("Quang Ngoc"),
                                                    ["rating_star"] = new OpenApiInteger(2),
                                                    ["comment"] = new OpenApiString("Plot hơi rối và khó hiểu, mình không thích nhiều tầng nghĩa kiểu này. 2 sao."),
                                                    ["rating_at"] = new OpenApiString("2025-09-27T00:00:00")
                                                }
                                            }
                                        }
                                    }
                                },
                                ["Success_EmptyList"] = new OpenApiExample
                                {
                                    Summary = "Success - No reviews yet",
                                    Description = "Phim chưa có review nào",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy danh sách review thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(999),
                                            ["page"] = new OpenApiInteger(1),
                                            ["limit"] = new OpenApiInteger(10),
                                            ["total_reviews"] = new OpenApiInteger(0),
                                            ["average_rating"] = new OpenApiNull(),
                                            ["items"] = new OpenApiArray()
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                // ===== BAD REQUEST (400) =====
                operation.Responses.Add("400", new OpenApiResponse
                {
                    Description = "Lỗi validation dữ liệu đầu vào",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["InvalidMovieId"] = new OpenApiExample
                                {
                                    Summary = "Invalid Movie ID",
                                    Description = "Movie ID không hợp lệ (phải > 0)",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lỗi xác thực dữ liệu"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["movieId"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Movie ID phải là số nguyên dương"),
                                                ["path"] = new OpenApiString("movieId"),
                                                ["location"] = new OpenApiString("path")
                                            }
                                        }
                                    }
                                },
                                ["InvalidPage"] = new OpenApiExample
                                {
                                    Summary = "Invalid page number",
                                    Description = "Page phải >= 1",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lỗi xác thực dữ liệu"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["page"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Page phải lớn hơn hoặc bằng 1"),
                                                ["path"] = new OpenApiString("page"),
                                                ["location"] = new OpenApiString("query")
                                            }
                                        }
                                    }
                                },
                                ["InvalidLimit"] = new OpenApiExample
                                {
                                    Summary = "Invalid limit value",
                                    Description = "Limit phải trong khoảng 1-100",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lỗi xác thực dữ liệu"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["limit"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Limit phải trong khoảng 1-100"),
                                                ["path"] = new OpenApiString("limit"),
                                                ["location"] = new OpenApiString("query")
                                            }
                                        }
                                    }
                                },
                                ["InvalidSort"] = new OpenApiExample
                                {
                                    Summary = "Invalid sort parameter",
                                    Description = "Sort phải là newest, oldest, highest, hoặc lowest",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lỗi xác thực dữ liệu"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["sort"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Sort phải là: newest, oldest, highest, hoặc lowest"),
                                                ["path"] = new OpenApiString("sort"),
                                                ["location"] = new OpenApiString("query")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                // ===== NOT FOUND (404) =====
                operation.Responses.Add("404", new OpenApiResponse
                {
                    Description = "Không tìm thấy phim",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["MovieNotFound"] = new OpenApiExample
                                {
                                    Summary = "Movie not found",
                                    Description = "Không tìm thấy phim với ID này",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Không tìm thấy phim"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["movie"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Không tìm thấy phim"),
                                                ["path"] = new OpenApiString("movieId"),
                                                ["location"] = new OpenApiString("path")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                // ===== SERVER ERROR (500) =====
                operation.Responses.Add("500", new OpenApiResponse
                {
                    Description = "Lỗi hệ thống",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["InternalServerError"] = new OpenApiExample
                                {
                                    Summary = "Internal server error",
                                    Description = "Lỗi không mong muốn từ phía server",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Đã xảy ra lỗi hệ thống trong quá trình lấy danh sách review.")
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }
    }
}

