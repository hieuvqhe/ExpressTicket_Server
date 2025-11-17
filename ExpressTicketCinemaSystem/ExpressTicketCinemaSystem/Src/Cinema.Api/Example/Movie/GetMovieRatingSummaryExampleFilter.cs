using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class GetMovieRatingSummaryExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // GET /cinema/movies/{movieId}/rating-summary
            if (method == "GET" && path?.Contains("{movieId}/rating-summary") == true)
            {
                // Xóa examples mặc định
                operation.Responses.Clear();

                // ===== SUCCESS RESPONSE (200 OK) =====
                operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "Lấy thống kê rating thành công",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Success_WithRatings"] = new OpenApiExample
                                {
                                    Summary = "Success - Movie with ratings",
                                    Description = "Phim có ratings với breakdown theo từng mức sao",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy thống kê rating thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(3),
                                            ["average_rating"] = new OpenApiDouble(8.5),
                                            ["total_ratings"] = new OpenApiInteger(54),
                                            ["breakdown"] = new OpenApiObject
                                            {
                                                ["5"] = new OpenApiInteger(30),
                                                ["4"] = new OpenApiInteger(15),
                                                ["3"] = new OpenApiInteger(6),
                                                ["2"] = new OpenApiInteger(2),
                                                ["1"] = new OpenApiInteger(1)
                                            }
                                        }
                                    }
                                },
                                ["Success_NoRatings"] = new OpenApiExample
                                {
                                    Summary = "Success - Movie with no ratings",
                                    Description = "Phim chưa có rating nào",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy thống kê rating thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(999),
                                            ["average_rating"] = new OpenApiNull(),
                                            ["total_ratings"] = new OpenApiInteger(0),
                                            ["breakdown"] = new OpenApiObject
                                            {
                                                ["5"] = new OpenApiInteger(0),
                                                ["4"] = new OpenApiInteger(0),
                                                ["3"] = new OpenApiInteger(0),
                                                ["2"] = new OpenApiInteger(0),
                                                ["1"] = new OpenApiInteger(0)
                                            }
                                        }
                                    }
                                },
                                ["Success_HighRated"] = new OpenApiExample
                                {
                                    Summary = "Success - Highly rated movie",
                                    Description = "Phim có rating cao, phần lớn 5 sao",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy thống kê rating thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(123),
                                            ["average_rating"] = new OpenApiDouble(4.8),
                                            ["total_ratings"] = new OpenApiInteger(2300),
                                            ["breakdown"] = new OpenApiObject
                                            {
                                                ["5"] = new OpenApiInteger(1840), // 80%
                                                ["4"] = new OpenApiInteger(345),  // 15%
                                                ["3"] = new OpenApiInteger(92),   // 4%
                                                ["2"] = new OpenApiInteger(18),   // <1%
                                                ["1"] = new OpenApiInteger(5)     // <1%
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
                                        ["message"] = new OpenApiString("Đã xảy ra lỗi hệ thống trong quá trình lấy thống kê rating.")
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


