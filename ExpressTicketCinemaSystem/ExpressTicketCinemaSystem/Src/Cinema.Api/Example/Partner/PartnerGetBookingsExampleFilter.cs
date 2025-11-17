using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class PartnerGetBookingsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Partners" || actionName != "GetPartnerBookings")
            {
                return;
            }

            // API này không có Request Body (chỉ có query params)

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with bookings
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Use Case 1: Partner xem tất cả đơn từ tất cả rạp
                    content.Examples.Add("Use Case 1 - All Bookings", new OpenApiExample
                    {
                        Summary = "Partner xem tất cả đơn từ tất cả rạp",
                        Description = "GET /partners/bookings",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 150,
                            "totalPages": 8,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_789456123",
                                "orderCode": "ORD20251116001",
                                "createdAt": "2025-11-16T20:14:30",
                                "updatedAt": "2025-11-16T20:15:10",
                                "ticketCount": 3,
                                "customer": {
                                  "customerId": 50,
                                  "userId": 100,
                                  "fullname": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0912345678"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                }
                              },
                              {
                                "bookingId": 124,
                                "bookingCode": "BK20251117XYZ456",
                                "bookingTime": "2025-11-17T15:30:00",
                                "totalAmount": 350000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_456789012",
                                "orderCode": "ORD20251117002",
                                "createdAt": "2025-11-17T15:29:00",
                                "updatedAt": "2025-11-17T15:30:30",
                                "ticketCount": 2,
                                "customer": {
                                  "customerId": 52,
                                  "userId": 102,
                                  "fullname": "Trần Thị B",
                                  "email": "tranthib@example.com",
                                  "phone": "0923456789"
                                },
                                "showtime": {
                                  "showtimeId": 1005,
                                  "showDatetime": "2025-11-21T21:00:00",
                                  "endTime": "2025-11-21T23:45:00",
                                  "formatType": "3D IMAX",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 6,
                                  "cinemaName": "CGV Vincom Gò Vấp",
                                  "address": "12 Phan Văn Trị, Gò Vấp",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Gò Vấp"
                                },
                                "movie": {
                                  "movieId": 15,
                                  "title": "Dune: Part Three",
                                  "durationMinutes": 165,
                                  "posterUrl": "https://example.com/posters/dune3.jpg",
                                  "genre": "Sci-Fi, Adventure, Drama"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Use Case 2: Partner xem đơn từ rạp cụ thể
                    content.Examples.Add("Use Case 2 - Specific Cinema", new OpenApiExample
                    {
                        Summary = "Partner xem đơn từ rạp cụ thể",
                        Description = "GET /partners/bookings?cinemaId=5",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 75,
                            "totalPages": 4,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_789456123",
                                "orderCode": "ORD20251116001",
                                "createdAt": "2025-11-16T20:14:30",
                                "updatedAt": "2025-11-16T20:15:10",
                                "ticketCount": 3,
                                "customer": {
                                  "customerId": 50,
                                  "userId": 100,
                                  "fullname": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0912345678"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Use Case 3: Partner tìm đơn theo khách hàng
                    content.Examples.Add("Use Case 3 - Search by Customer", new OpenApiExample
                    {
                        Summary = "Partner tìm đơn theo email khách hàng",
                        Description = "GET /partners/bookings?customerEmail=nguyenvana@example.com",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 12,
                            "totalPages": 1,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_789456123",
                                "orderCode": "ORD20251116001",
                                "createdAt": "2025-11-16T20:14:30",
                                "updatedAt": "2025-11-16T20:15:10",
                                "ticketCount": 3,
                                "customer": {
                                  "customerId": 50,
                                  "userId": 100,
                                  "fullname": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0912345678"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                }
                              },
                              {
                                "bookingId": 115,
                                "bookingCode": "BK20251110DEF789",
                                "bookingTime": "2025-11-10T14:20:00",
                                "totalAmount": 180000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_123123123",
                                "orderCode": "ORD20251110001",
                                "createdAt": "2025-11-10T14:19:00",
                                "updatedAt": "2025-11-10T14:20:30",
                                "ticketCount": 2,
                                "customer": {
                                  "customerId": 50,
                                  "userId": 100,
                                  "fullname": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0912345678"
                                },
                                "showtime": {
                                  "showtimeId": 998,
                                  "showDatetime": "2025-11-12T18:00:00",
                                  "endTime": "2025-11-12T20:20:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "movie": {
                                  "movieId": 12,
                                  "title": "Spider-Man: Beyond",
                                  "durationMinutes": 140,
                                  "posterUrl": "https://example.com/posters/spiderman.jpg",
                                  "genre": "Action, Adventure, Superhero"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Use Case 4: Partner xem đơn đã thanh toán trong tháng
                    content.Examples.Add("Use Case 4 - Paid Orders in Month", new OpenApiExample
                    {
                        Summary = "Partner xem đơn đã thanh toán trong tháng",
                        Description = "GET /partners/bookings?status=PAID&fromDate=2025-11-01&toDate=2025-11-30",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 85,
                            "totalPages": 5,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_789456123",
                                "orderCode": "ORD20251116001",
                                "createdAt": "2025-11-16T20:14:30",
                                "updatedAt": "2025-11-16T20:15:10",
                                "ticketCount": 3,
                                "customer": {
                                  "customerId": 50,
                                  "userId": 100,
                                  "fullname": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0912345678"
                                },
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 5,
                                  "cinemaName": "CGV Vincom Thủ Đức",
                                  "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Thủ Đức"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg",
                                  "genre": "Action, Adventure, Sci-Fi"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Example: Empty list
                    content.Examples.Add("Empty List", new OpenApiExample
                    {
                        Summary = "Partner chưa có đơn hàng nào",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
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
                    
                    // Example 1: Cinema not owned by partner
                    content.Examples.Add("Cinema Not Owned", new OpenApiExample
                    {
                        Summary = "Rạp không thuộc về partner",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "cinemaId": {
                              "msg": "Rạp này không thuộc về đối tác của bạn.",
                              "path": "cinemaId",
                              "location": "body"
                            }
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Invalid sort parameters
                    content.Examples.Add("Invalid Sort", new OpenApiExample
                    {
                        Summary = "Tham số sắp xếp không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "sortBy": {
                              "msg": "SortBy phải là một trong: booking_time, total_amount, created_at.",
                              "path": "sortBy",
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
                        Summary = "Không có token hoặc token không hợp lệ",
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

            // Response 404 Not Found
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Partner Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy thông tin đối tác",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy thông tin đối tác hoặc tài khoản chưa được kích hoạt."
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

