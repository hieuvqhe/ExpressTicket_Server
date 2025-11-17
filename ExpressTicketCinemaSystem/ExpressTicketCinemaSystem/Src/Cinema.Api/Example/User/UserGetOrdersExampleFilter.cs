using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserGetOrdersExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "GetOrders")
            {
                return;
            }

            // API này không có Request Body (chỉ có query params)

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with orders
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Success with multiple orders
                    content.Examples.Add("Success - Multiple Orders", new OpenApiExample
                    {
                        Summary = "Danh sách đơn hàng với nhiều booking",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 10,
                            "totalItems": 42,
                            "totalPages": 5,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "totalAmount": 250000,
                                "ticketCount": 3,
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "formatType": "2D"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Thủ Đức",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                }
                              },
                              {
                                "bookingId": 122,
                                "bookingCode": "BK20251115XYZ789",
                                "bookingTime": "2025-11-15T14:30:00",
                                "status": "PENDING",
                                "state": "PENDING_PAYMENT",
                                "paymentStatus": "PENDING",
                                "totalAmount": 180000,
                                "ticketCount": 2,
                                "showtime": {
                                  "showtimeId": 1002,
                                  "showDatetime": "2025-11-18T21:00:00",
                                  "formatType": "3D IMAX"
                                },
                                "movie": {
                                  "movieId": 15,
                                  "title": "Dune: Part Three",
                                  "durationMinutes": 165,
                                  "posterUrl": "https://example.com/posters/dune.jpg"
                                },
                                "cinema": {
                                  "cinemaId": 8,
                                  "cinemaName": "Galaxy Nguyễn Du",
                                  "address": "116 Nguyễn Du, Quận 1",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 1"
                                }
                              },
                              {
                                "bookingId": 118,
                                "bookingCode": "BK20251110DEF456",
                                "bookingTime": "2025-11-10T10:20:00",
                                "status": "CANCELLED",
                                "state": "CANCELLED",
                                "paymentStatus": "FAILED",
                                "totalAmount": 200000,
                                "ticketCount": 2,
                                "showtime": {
                                  "showtimeId": 998,
                                  "showDatetime": "2025-11-12T18:00:00",
                                  "formatType": "2D"
                                },
                                "movie": {
                                  "movieId": 12,
                                  "title": "Spider-Man: Beyond",
                                  "durationMinutes": 140,
                                  "posterUrl": "https://example.com/posters/spiderman.jpg"
                                },
                                "cinema": {
                                  "cinemaId": 3,
                                  "cinemaName": "Lotte Cinema Nowzone",
                                  "address": "235 Nguyễn Văn Cừ, Quận 5",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 5"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Empty list (no orders)
                    content.Examples.Add("Success - Empty List", new OpenApiExample
                    {
                        Summary = "User chưa có đơn hàng nào",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 10,
                            "totalItems": 0,
                            "totalPages": 0,
                            "items": []
                          }
                        }
                        """
                        )
                    });

                    // Example 3: Filtered by status
                    content.Examples.Add("Success - Filtered by PAID", new OpenApiExample
                    {
                        Summary = "Lọc chỉ các đơn đã thanh toán",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 10,
                            "totalItems": 15,
                            "totalPages": 2,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "totalAmount": 250000,
                                "ticketCount": 3,
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "formatType": "2D"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Thủ Đức",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request - Validation errors
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Invalid page number
                    content.Examples.Add("Invalid Page", new OpenApiExample
                    {
                        Summary = "Page số không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "page": {
                              "msg": "Page phải lớn hơn hoặc bằng 1.",
                              "path": "page",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Invalid page size
                    content.Examples.Add("Invalid PageSize", new OpenApiExample
                    {
                        Summary = "PageSize không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "pageSize": {
                              "msg": "PageSize phải trong khoảng 1-100.",
                              "path": "pageSize",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 401 Unauthorized - Invalid token
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Missing token
                    content.Examples.Add("Missing Token", new OpenApiExample
                    {
                        Summary = "Không có token",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Token không hợp lệ hoặc không chứa ID người dùng.",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Expired token
                    content.Examples.Add("Expired Token", new OpenApiExample
                    {
                        Summary = "Token đã hết hạn",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "auth": {
                              "msg": "Token đã hết hạn.",
                              "path": "form",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 404 Not Found - User not found
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("User Not Found", new OpenApiExample
                    {
                        Summary = "Người dùng không tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Người dùng không tồn tại."
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Summary = "Lỗi server",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi khi lấy danh sách đơn hàng: An unexpected error occurred."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}

