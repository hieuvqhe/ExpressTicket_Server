using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserGetTicketsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "GetTickets")
            {
                return;
            }

            // API này không có Request Body (chỉ có query params)

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with tickets
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Upcoming tickets
                    content.Examples.Add("Success - Upcoming Tickets", new OpenApiExample
                    {
                        Summary = "Vé sắp chiếu (type=upcoming)",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách vé thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 5,
                            "totalPages": 1,
                            "items": [
                              {
                                "ticketId": 1001,
                                "price": 85000,
                                "status": "ACTIVE",
                                "booking": {
                                  "bookingId": 123,
                                  "bookingCode": "BK20251116ABC123",
                                  "paymentStatus": "PAID"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "seat": {
                                  "seatId": 101,
                                  "rowCode": "D",
                                  "seatNumber": 7,
                                  "seatName": "D7",
                                  "seatTypeName": "Standard"
                                }
                              },
                              {
                                "ticketId": 1002,
                                "price": 85000,
                                "status": "ACTIVE",
                                "booking": {
                                  "bookingId": 123,
                                  "bookingCode": "BK20251116ABC123",
                                  "paymentStatus": "PAID"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "seat": {
                                  "seatId": 102,
                                  "rowCode": "D",
                                  "seatNumber": 8,
                                  "seatName": "D8",
                                  "seatTypeName": "Standard"
                                }
                              },
                              {
                                "ticketId": 1010,
                                "price": 175000,
                                "status": "ACTIVE",
                                "booking": {
                                  "bookingId": 124,
                                  "bookingCode": "BK20251117XYZ456",
                                  "paymentStatus": "PAID"
                                },
                                "movie": {
                                  "movieId": 15,
                                  "title": "Dune: Part Three",
                                  "durationMinutes": 165,
                                  "posterUrl": "https://example.com/posters/dune3.jpg",
                                  "genre": "Sci-Fi, Adventure, Drama"
                                },
                                "cinema": {
                                  "cinemaId": 8,
                                  "cinemaName": "Galaxy Nguyễn Du",
                                  "address": "116 Nguyễn Du, Phường Bến Thành",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 1"
                                },
                                "showtime": {
                                  "showtimeId": 1002,
                                  "showDatetime": "2025-11-21T21:00:00",
                                  "endTime": "2025-11-21T23:45:00",
                                  "formatType": "3D IMAX",
                                  "status": "ACTIVE"
                                },
                                "seat": {
                                  "seatId": 201,
                                  "rowCode": "G",
                                  "seatNumber": 10,
                                  "seatName": "G10",
                                  "seatTypeName": "VIP"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Past tickets
                    content.Examples.Add("Success - Past Tickets", new OpenApiExample
                    {
                        Summary = "Vé đã chiếu (type=past)",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách vé thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 8,
                            "totalPages": 1,
                            "items": [
                              {
                                "ticketId": 901,
                                "price": 80000,
                                "status": "USED",
                                "booking": {
                                  "bookingId": 110,
                                  "bookingCode": "BK20251110ABC111",
                                  "paymentStatus": "PAID"
                                },
                                "movie": {
                                  "movieId": 8,
                                  "title": "The Batman Returns",
                                  "durationMinutes": 140,
                                  "posterUrl": "https://example.com/posters/batman.jpg",
                                  "genre": "Action, Crime, Drama"
                                },
                                "cinema": {
                                  "cinemaId": 3,
                                  "cinemaName": "Lotte Cinema Nowzone",
                                  "address": "235 Nguyễn Văn Cừ, Phường 4",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 5"
                                },
                                "showtime": {
                                  "showtimeId": 950,
                                  "showDatetime": "2025-11-10T18:00:00",
                                  "endTime": "2025-11-10T20:20:00",
                                  "formatType": "2D",
                                  "status": "ENDED"
                                },
                                "seat": {
                                  "seatId": 150,
                                  "rowCode": "E",
                                  "seatNumber": 5,
                                  "seatName": "E5",
                                  "seatTypeName": "Standard"
                                }
                              },
                              {
                                "ticketId": 902,
                                "price": 80000,
                                "status": "USED",
                                "booking": {
                                  "bookingId": 110,
                                  "bookingCode": "BK20251110ABC111",
                                  "paymentStatus": "PAID"
                                },
                                "movie": {
                                  "movieId": 8,
                                  "title": "The Batman Returns",
                                  "durationMinutes": 140,
                                  "posterUrl": "https://example.com/posters/batman.jpg",
                                  "genre": "Action, Crime, Drama"
                                },
                                "cinema": {
                                  "cinemaId": 3,
                                  "cinemaName": "Lotte Cinema Nowzone",
                                  "address": "235 Nguyễn Văn Cừ, Phường 4",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 5"
                                },
                                "showtime": {
                                  "showtimeId": 950,
                                  "showDatetime": "2025-11-10T18:00:00",
                                  "endTime": "2025-11-10T20:20:00",
                                  "formatType": "2D",
                                  "status": "ENDED"
                                },
                                "seat": {
                                  "seatId": 151,
                                  "rowCode": "E",
                                  "seatNumber": 6,
                                  "seatName": "E6",
                                  "seatTypeName": "Standard"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Example 3: Empty list
                    content.Examples.Add("Success - Empty List", new OpenApiExample
                    {
                        Summary = "Không có vé",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách vé thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 0,
                            "totalPages": 0,
                            "items": []
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
                    
                    // Example 1: Invalid type
                    content.Examples.Add("Invalid Type", new OpenApiExample
                    {
                        Summary = "Type không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "type": {
                              "msg": "Type phải là một trong: upcoming, past, all.",
                              "path": "type",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Invalid page
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

                    // Example 3: Invalid page size
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
                          "message": "Lỗi khi lấy danh sách vé: An unexpected error occurred."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}

