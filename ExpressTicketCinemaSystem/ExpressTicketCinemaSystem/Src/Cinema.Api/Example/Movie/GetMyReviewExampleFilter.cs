using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Movie
{
    public class GetMyReviewExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Movies") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            // GET /cinema/movies/{movieId}/my-review
            if (method == "GET" && path?.Contains("{movieId}/my-review") == true)
            {
                // Xóa examples mặc định
                operation.Responses.Clear();

                // ===== SUCCESS RESPONSE (200 OK) =====
                operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "Lấy review của bạn thành công",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Success_HasReview"] = new OpenApiExample
                                {
                                    Summary = "Success - User has reviewed",
                                    Description = "User đã review phim này",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy review của bạn thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(123),
                                            ["user_id"] = new OpenApiInteger(10),
                                            ["review"] = new OpenApiObject
                                            {
                                                ["rating_id"] = new OpenApiInteger(999),
                                                ["rating_star"] = new OpenApiInteger(4),
                                                ["comment"] = new OpenApiString("Phim ổn, kỹ xảo tốt."),
                                                ["rating_at"] = new OpenApiString("2025-11-16T09:00:00Z")
                                            }
                                        }
                                    }
                                },
                                ["Success_NoReview"] = new OpenApiExample
                                {
                                    Summary = "Success - User has not reviewed",
                                    Description = "User chưa review phim này",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Lấy review của bạn thành công"),
                                        ["result"] = new OpenApiObject
                                        {
                                            ["movie_id"] = new OpenApiInteger(123),
                                            ["user_id"] = new OpenApiInteger(10),
                                            ["review"] = new OpenApiNull()
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                // ===== UNAUTHORIZED (401) =====
                operation.Responses.Add("401", new OpenApiResponse
                {
                    Description = "Chưa đăng nhập hoặc token không hợp lệ",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Unauthorized_NoToken"] = new OpenApiExample
                                {
                                    Summary = "No authentication token",
                                    Description = "Chưa đăng nhập, không có token",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Xác thực thất bại"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["auth"] = new OpenApiObject
                                            {
                                                ["msg"] = new OpenApiString("Không thể xác định người dùng từ token"),
                                                ["path"] = new OpenApiString("token"),
                                                ["location"] = new OpenApiString("header")
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
                                        ["message"] = new OpenApiString("Đã xảy ra lỗi hệ thống trong quá trình lấy review của bạn.")
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


