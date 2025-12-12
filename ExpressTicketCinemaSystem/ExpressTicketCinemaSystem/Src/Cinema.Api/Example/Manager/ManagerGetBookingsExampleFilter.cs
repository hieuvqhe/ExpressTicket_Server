using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerGetBookingsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "GetBookings")
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
                    
                    // Use Case 1: Manager xem tất cả đơn hàng
                    content.Examples.Add("Use Case 1 - All Bookings", new OpenApiExample
                    {
                        Summary = "Manager xem tất cả đơn hàng từ tất cả partner",
                        Description = "GET /api/manager/bookings",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 5000,
                            "totalPages": 250,
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
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
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
                                  "cinemaId": 8,
                                  "cinemaName": "Galaxy Nguyễn Du",
                                  "address": "116 Nguyễn Du, Phường Bến Thành",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Quận 1"
                                },
                                "partner": {
                                  "partnerId": 2,
                                  "partnerName": "Galaxy Cinema",
                                  "taxCode": "9876543210"
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

                    // Use Case 2: Manager xem đơn của Partner cụ thể
                    content.Examples.Add("Use Case 2 - Partner Filter", new OpenApiExample
                    {
                        Summary = "Manager xem đơn của Partner 1",
                        Description = "GET /api/manager/bookings?partnerId=1",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 1500,
                            "totalPages": 75,
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
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
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

                    // Use Case 3: Manager tìm đơn theo khách hàng
                    content.Examples.Add("Use Case 3 - Search by Customer", new OpenApiExample
                    {
                        Summary = "Manager tìm đơn theo email khách hàng",
                        Description = "GET /api/manager/bookings?customerEmail=nguyenvana@example.com",
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
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
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

                    // Use Case 4: Manager xem đơn đã thanh toán trong tháng
                    content.Examples.Add("Use Case 4 - Paid Orders in Month", new OpenApiExample
                    {
                        Summary = "Manager xem đơn đã thanh toán trong tháng 11",
                        Description = "GET /api/manager/bookings?status=PAID&fromDate=2025-11-01&toDate=2025-11-30",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 3500,
                            "totalPages": 175,
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
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
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

                    // Use Case 5: Manager xem đơn theo phim
                    content.Examples.Add("Use Case 5 - Filter by Movie", new OpenApiExample
                    {
                        Summary = "Manager xem đơn của phim Avengers",
                        Description = "GET /api/manager/bookings?movieId=10",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 850,
                            "totalPages": 43,
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
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
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

                    // Use Case 6: Manager xem đơn có giá trị cao
                    content.Examples.Add("Use Case 6 - High Value Orders", new OpenApiExample
                    {
                        Summary = "Manager xem đơn có giá trị >= 500k",
                        Description = "GET /api/manager/bookings?minAmount=500000&sortBy=total_amount&sortOrder=desc",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách đơn hàng thành công.",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 120,
                            "totalPages": 6,
                            "items": [
                              {
                                "bookingId": 200,
                                "bookingCode": "BK20251118VIP999",
                                "bookingTime": "2025-11-18T18:00:00",
                                "totalAmount": 1200000,
                                "status": "PAID",
                                "state": "COMPLETED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "paymentTxId": "payos_tx_999999999",
                                "orderCode": "ORD20251118099",
                                "createdAt": "2025-11-18T17:59:00",
                                "updatedAt": "2025-11-18T18:00:30",
                                "ticketCount": 8,
                                "customer": {
                                  "customerId": 80,
                                  "userId": 150,
                                  "fullname": "Lê Văn C",
                                  "email": "levanc@example.com",
                                  "phone": "0934567890"
                                },
                                "showtime": {
                                  "showtimeId": 1050,
                                  "showDatetime": "2025-11-25T20:00:00",
                                  "endTime": "2025-11-25T23:00:00",
                                  "formatType": "4DX",
                                  "status": "ACTIVE"
                                },
                                "cinema": {
                                  "cinemaId": 10,
                                  "cinemaName": "CGV Landmark 81",
                                  "address": "208 Nguyễn Hữu Cảnh, Bình Thạnh",
                                  "city": "TP. Hồ Chí Minh",
                                  "district": "Bình Thạnh"
                                },
                                "partner": {
                                  "partnerId": 1,
                                  "partnerName": "CGV Vietnam",
                                  "taxCode": "0123456789"
                                },
                                "movie": {
                                  "movieId": 20,
                                  "title": "Avatar 3: The Way of Water Returns",
                                  "durationMinutes": 180,
                                  "posterUrl": "https://example.com/posters/avatar3.jpg",
                                  "genre": "Sci-Fi, Adventure, Fantasy"
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
                    
                    content.Examples.Add("Invalid Sort", new OpenApiExample
                    {
                        Summary = "Tham số sắp xếp không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "sortBy": {
                              "msg": "SortBy phải là một trong: booking_time, total_amount, created_at, customer_name, partner_name, cinema_name.",
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

            // Response 401 Unauthorized
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

            // Response 403 Forbidden
            if (operation.Responses.ContainsKey("403"))
            {
                var response = operation.Responses["403"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Not Manager", new OpenApiExample
                    {
                        Summary = "Không phải Manager",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Bạn không có quyền truy cập. Chỉ Manager mới có thể xem tất cả đơn hàng.",
                          "errors": {
                            "auth": {
                              "msg": "Bạn không có quyền truy cập. Chỉ Manager mới có thể xem tất cả đơn hàng.",
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

