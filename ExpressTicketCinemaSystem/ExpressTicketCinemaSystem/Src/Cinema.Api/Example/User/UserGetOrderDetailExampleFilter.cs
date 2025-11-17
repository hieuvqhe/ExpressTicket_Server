using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.User
{
    public class UserGetOrderDetailExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "User" || actionName != "GetOrderDetail")
            {
                return;
            }

            // API này không có Request Body

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with order detail
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Paid order with tickets
                    content.Examples.Add("Success - Paid Order", new OpenApiExample
                    {
                        Summary = "Đơn hàng đã thanh toán với vé",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 123,
                              "bookingCode": "BK20251116ABC123",
                              "bookingTime": "2025-11-16T20:15:00",
                              "totalAmount": 250000,
                              "status": "PAID",
                              "state": "COMPLETED",
                              "paymentStatus": "PAID",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": "payos_tx_789456123",
                              "voucherId": 5,
                              "orderCode": "ORD20251116001",
                              "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                              "createdAt": "2025-11-16T20:14:30",
                              "updatedAt": "2025-11-16T20:15:10"
                            },
                            "showtime": {
                              "showtimeId": 1001,
                              "showDatetime": "2025-11-20T19:30:00",
                              "endTime": "2025-11-20T22:00:00",
                              "status": "ACTIVE",
                              "basePrice": 80000,
                              "formatType": "2D"
                            },
                            "movie": {
                              "movieId": 10,
                              "title": "Avengers: Secret Wars",
                              "genre": "Action, Adventure, Sci-Fi",
                              "durationMinutes": 150,
                              "language": "English",
                              "director": "Destin Daniel Cretton",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/avengers.jpg",
                              "bannerUrl": "https://example.com/banners/avengers-wide.jpg",
                              "description": "The Avengers face their greatest threat yet as they battle across the multiverse."
                            },
                            "cinema": {
                              "cinemaId": 5,
                              "cinemaName": "CGV Vincom Thủ Đức",
                              "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Thủ Đức",
                              "phone": "0287 123 4567",
                              "email": "thuduc@cgv.vn"
                            },
                            "tickets": [
                              {
                                "ticketId": 1001,
                                "price": 85000,
                                "status": "ACTIVE",
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
                                "seat": {
                                  "seatId": 102,
                                  "rowCode": "D",
                                  "seatNumber": 8,
                                  "seatName": "D8",
                                  "seatTypeName": "Standard"
                                }
                              },
                              {
                                "ticketId": 1003,
                                "price": 80000,
                                "status": "ACTIVE",
                                "seat": {
                                  "seatId": 103,
                                  "rowCode": "D",
                                  "seatNumber": 9,
                                  "seatName": "D9",
                                  "seatTypeName": "Standard"
                                }
                              }
                            ],
                            "voucher": {
                              "voucherId": 5,
                              "voucherCode": "SUMMER2025",
                              "discountType": "percent",
                              "discountVal": 10.0
                            }
                          }
                        }
                        """
                        )
                    });

                    // Example 2: Paid order with VIP seats
                    content.Examples.Add("Success - VIP Seats", new OpenApiExample
                    {
                        Summary = "Đơn hàng với ghế VIP",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 124,
                              "bookingCode": "BK20251117XYZ456",
                              "bookingTime": "2025-11-17T15:30:00",
                              "totalAmount": 350000,
                              "status": "PAID",
                              "state": "COMPLETED",
                              "paymentStatus": "PAID",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": "payos_tx_456789012",
                              "voucherId": null,
                              "orderCode": "ORD20251117002",
                              "sessionId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                              "createdAt": "2025-11-17T15:29:00",
                              "updatedAt": "2025-11-17T15:30:30"
                            },
                            "showtime": {
                              "showtimeId": 1002,
                              "showDatetime": "2025-11-18T21:00:00",
                              "endTime": "2025-11-18T23:45:00",
                              "status": "ACTIVE",
                              "basePrice": 100000,
                              "formatType": "3D IMAX"
                            },
                            "movie": {
                              "movieId": 15,
                              "title": "Dune: Part Three",
                              "genre": "Sci-Fi, Adventure, Drama",
                              "durationMinutes": 165,
                              "language": "English",
                              "director": "Denis Villeneuve",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/dune3.jpg",
                              "bannerUrl": "https://example.com/banners/dune3-wide.jpg",
                              "description": "Paul Atreides continues his epic journey across the desert planet Arrakis."
                            },
                            "cinema": {
                              "cinemaId": 8,
                              "cinemaName": "Galaxy Nguyễn Du",
                              "address": "116 Nguyễn Du, Phường Bến Thành",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Quận 1",
                              "phone": "0283 822 2299",
                              "email": "nguyendu@galaxycinema.vn"
                            },
                            "tickets": [
                              {
                                "ticketId": 1010,
                                "price": 175000,
                                "status": "ACTIVE",
                                "seat": {
                                  "seatId": 201,
                                  "rowCode": "G",
                                  "seatNumber": 10,
                                  "seatName": "G10",
                                  "seatTypeName": "VIP"
                                }
                              },
                              {
                                "ticketId": 1011,
                                "price": 175000,
                                "status": "ACTIVE",
                                "seat": {
                                  "seatId": 202,
                                  "rowCode": "G",
                                  "seatNumber": 11,
                                  "seatName": "G11",
                                  "seatTypeName": "VIP"
                                }
                              }
                            ],
                            "voucher": null
                          }
                        }
                        """
                        )
                    });

                    // Example 3: Pending payment (no tickets yet)
                    content.Examples.Add("Pending Payment", new OpenApiExample
                    {
                        Summary = "Đơn hàng chưa thanh toán (chưa có vé)",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 125,
                              "bookingCode": "BK20251117DEF789",
                              "bookingTime": "2025-11-17T16:45:00",
                              "totalAmount": 180000,
                              "status": "PENDING",
                              "state": "PENDING_PAYMENT",
                              "paymentStatus": "PENDING",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": null,
                              "voucherId": null,
                              "orderCode": "ORD20251117003",
                              "sessionId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
                              "createdAt": "2025-11-17T16:44:00",
                              "updatedAt": "2025-11-17T16:45:00"
                            },
                            "showtime": {
                              "showtimeId": 1003,
                              "showDatetime": "2025-11-19T14:00:00",
                              "endTime": "2025-11-19T16:20:00",
                              "status": "ACTIVE",
                              "basePrice": 90000,
                              "formatType": "2D"
                            },
                            "movie": {
                              "movieId": 12,
                              "title": "Spider-Man: Beyond",
                              "genre": "Action, Adventure, Superhero",
                              "durationMinutes": 140,
                              "language": "English",
                              "director": "Jon Watts",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/spiderman.jpg",
                              "bannerUrl": "https://example.com/banners/spiderman-wide.jpg",
                              "description": "Spider-Man faces new challenges across dimensions."
                            },
                            "cinema": {
                              "cinemaId": 3,
                              "cinemaName": "Lotte Cinema Nowzone",
                              "address": "235 Nguyễn Văn Cừ, Phường 4",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Quận 5",
                              "phone": "0286 293 8888",
                              "email": "nowzone@lottecinema.vn"
                            },
                            "tickets": [],
                            "voucher": null
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

            // Response 404 Not Found
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Example 1: Booking not found
                    content.Examples.Add("Booking Not Found", new OpenApiExample
                    {
                        Summary = "Đơn hàng không tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này."
                        }
                        """
                        )
                    });

                    // Example 2: User not found
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

                    // Example 3: No customer record
                    content.Examples.Add("Customer Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy thông tin khách hàng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy thông tin khách hàng."
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
                          "message": "Lỗi khi lấy chi tiết đơn hàng: An unexpected error occurred."
                        }
                        """
                        )
                    });
                }
            }
        }
    }
}

