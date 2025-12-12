using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Cashier
{
    public class CashierGetBookingsExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Cashier" || actionName != "GetBookings")
            {
                return;
            }

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with bookings
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Success with multiple bookings
                    content.Examples.Add("Success - Multiple Bookings", new OpenApiExample
                    {
                        Summary = "Danh sách booking với nhiều đơn hàng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách booking thành công",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 45,
                            "totalPages": 3,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "orderCode": "ORD20251116001",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "FULLY_CHECKED_IN",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "ticketCount": 3,
                                "checkedInTicketCount": 3,
                                "notCheckedInTicketCount": 0,
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg"
                                },
                                "customer": {
                                  "customerId": 50,
                                  "fullName": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0901234567"
                                }
                              },
                              {
                                "bookingId": 124,
                                "bookingCode": "BK20251117XYZ456",
                                "orderCode": "ORD20251117002",
                                "bookingTime": "2025-11-17T15:30:00",
                                "totalAmount": 350000,
                                "status": "PARTIAL_CHECKED_IN",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "ticketCount": 2,
                                "checkedInTicketCount": 1,
                                "notCheckedInTicketCount": 1,
                                "showtime": {
                                  "showtimeId": 1002,
                                  "showDatetime": "2025-11-18T21:00:00",
                                  "endTime": "2025-11-18T23:45:00",
                                  "formatType": "3D IMAX",
                                  "status": "ACTIVE"
                                },
                                "movie": {
                                  "movieId": 15,
                                  "title": "Dune: Part Three",
                                  "durationMinutes": 165,
                                  "posterUrl": "https://example.com/posters/dune3.jpg"
                                },
                                "customer": {
                                  "customerId": 51,
                                  "fullName": "Trần Thị B",
                                  "email": "tranthib@example.com",
                                  "phone": "0907654321"
                                }
                              },
                              {
                                "bookingId": 125,
                                "bookingCode": "BK20251118DEF789",
                                "orderCode": "ORD20251118003",
                                "bookingTime": "2025-11-18T10:20:00",
                                "totalAmount": 180000,
                                "status": "CONFIRMED",
                                "paymentStatus": "PAID",
                                "paymentProvider": "CASH",
                                "ticketCount": 2,
                                "checkedInTicketCount": 0,
                                "notCheckedInTicketCount": 2,
                                "showtime": {
                                  "showtimeId": 1003,
                                  "showDatetime": "2025-11-22T20:00:00",
                                  "endTime": "2025-11-22T22:30:00",
                                  "formatType": "3D",
                                  "status": "ACTIVE"
                                },
                                "movie": {
                                  "movieId": 20,
                                  "title": "Interstellar 2",
                                  "durationMinutes": 180,
                                  "posterUrl": "https://example.com/posters/interstellar2.jpg"
                                },
                                "customer": {
                                  "customerId": 52,
                                  "fullName": "Lê Văn C",
                                  "email": "levanc@example.com",
                                  "phone": "0912345678"
                                }
                              }
                            ]
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Empty list
                    content.Examples.Add("Success - Empty List", new OpenApiExample
                    {
                        Summary = "Không có booking nào",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách booking thành công",
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

                    // Example 3: Filtered by status
                    content.Examples.Add("Success - Filtered by FULLY_CHECKED_IN", new OpenApiExample
                    {
                        Summary = "Lọc chỉ các booking đã check-in đầy đủ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách booking thành công",
                          "result": {
                            "page": 1,
                            "pageSize": 20,
                            "totalItems": 12,
                            "totalPages": 1,
                            "items": [
                              {
                                "bookingId": 123,
                                "bookingCode": "BK20251116ABC123",
                                "orderCode": "ORD20251116001",
                                "bookingTime": "2025-11-16T20:15:00",
                                "totalAmount": 250000,
                                "status": "FULLY_CHECKED_IN",
                                "paymentStatus": "PAID",
                                "paymentProvider": "PAYOS",
                                "ticketCount": 3,
                                "checkedInTicketCount": 3,
                                "notCheckedInTicketCount": 0,
                                "showtime": {
                                  "showtimeId": 1001,
                                  "showDatetime": "2025-11-20T19:30:00",
                                  "endTime": "2025-11-20T22:00:00",
                                  "formatType": "2D",
                                  "status": "ACTIVE"
                                },
                                "movie": {
                                  "movieId": 10,
                                  "title": "Avengers: Secret Wars",
                                  "durationMinutes": 150,
                                  "posterUrl": "https://example.com/posters/avengers.jpg"
                                },
                                "customer": {
                                  "customerId": 50,
                                  "fullName": "Nguyễn Văn A",
                                  "email": "nguyenvana@example.com",
                                  "phone": "0901234567"
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
                    
                    // Example 1: Invalid page
                    content.Examples.Add("Invalid Page", new OpenApiExample
                    {
                        Summary = "Page số không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Page phải lớn hơn hoặc bằng 1.",
                          "errors": {
                            "page": {
                              "msg": "Page phải lớn hơn hoặc bằng 1.",
                              "path": "page",
                              "location": "query"
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
                          "message": "PageSize phải trong khoảng 1-100.",
                          "errors": {
                            "pageSize": {
                              "msg": "PageSize phải trong khoảng 1-100.",
                              "path": "pageSize",
                              "location": "query"
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
                        Summary = "Không có token hoặc không phải Cashier",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Thu ngân không có quyền truy cập rạp này"
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
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách booking."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}



















