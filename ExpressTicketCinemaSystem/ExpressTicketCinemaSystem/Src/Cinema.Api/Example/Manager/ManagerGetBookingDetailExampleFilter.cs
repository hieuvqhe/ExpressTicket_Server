using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Manager
{
    public class ManagerGetBookingDetailExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            if (controllerName != "Manager" || actionName != "GetBookingDetail")
            {
                return;
            }

            // API này không có Request Body (chỉ có path param bookingId)

            // ==================== RESPONSE EXAMPLES ====================

            // Response 200 OK - Success with full booking details
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    
                    // Use Case 1: Complete booking with payment, voucher, tickets and services
                    content.Examples.Add("Use Case 1 - Complete Booking", new OpenApiExample
                    {
                        Summary = "Đơn hàng hoàn chỉnh với voucher, combo và payment",
                        Description = "GET /api/manager/bookings/123",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 123,
                              "bookingCode": "BK20251116ABC123",
                              "bookingTime": "2025-11-16T20:15:00",
                              "totalAmount": 230000,
                              "status": "PAID",
                              "state": "COMPLETED",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": "payos_tx_789456123",
                              "paymentStatus": "PAID",
                              "orderCode": "ORD20251116001",
                              "sessionId": "550e8400-e29b-41d4-a716-446655440000",
                              "createdAt": "2025-11-16T20:14:30",
                              "updatedAt": "2025-11-16T20:15:10"
                            },
                            "customer": {
                              "customerId": 50,
                              "userId": 100,
                              "fullname": "Nguyễn Văn A",
                              "email": "nguyenvana@example.com",
                              "phone": "0912345678",
                              "username": "nguyenvana"
                            },
                            "showtime": {
                              "showtimeId": 1001,
                              "showDatetime": "2025-11-20T19:30:00",
                              "endTime": "2025-11-20T22:00:00",
                              "basePrice": 80000,
                              "status": "ACTIVE",
                              "formatType": "2D",
                              "availableSeats": 85
                            },
                            "cinema": {
                              "cinemaId": 5,
                              "cinemaName": "CGV Vincom Thủ Đức",
                              "address": "123 Võ Văn Ngân, Phường Linh Chiểu",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Thủ Đức",
                              "code": "CGV-THD-01",
                              "isActive": true
                            },
                            "screen": {
                              "screenId": 101,
                              "screenName": "Phòng 1",
                              "code": "P01",
                              "capacity": 120,
                              "screenType": "Standard",
                              "soundSystem": "Dolby Atmos"
                            },
                            "partner": {
                              "partnerId": 1,
                              "partnerName": "CGV Vietnam",
                              "taxCode": "0123456789",
                              "status": "approved",
                              "commissionRate": 10.0
                            },
                            "movie": {
                              "movieId": 10,
                              "title": "Avengers: Secret Wars",
                              "genre": "Action, Adventure, Sci-Fi",
                              "durationMinutes": 150,
                              "director": "Russo Brothers",
                              "language": "English",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/avengers.jpg",
                              "premiereDate": "2025-11-15",
                              "endDate": "2025-12-31"
                            },
                            "tickets": [
                              {
                                "ticketId": 501,
                                "seatId": 201,
                                "seatName": "A1",
                                "rowCode": "A",
                                "seatNumber": 1,
                                "seatTypeName": "VIP",
                                "price": 100000,
                                "status": "SOLD"
                              },
                              {
                                "ticketId": 502,
                                "seatId": 202,
                                "seatName": "A2",
                                "rowCode": "A",
                                "seatNumber": 2,
                                "seatTypeName": "VIP",
                                "price": 100000,
                                "status": "SOLD"
                              },
                              {
                                "ticketId": 503,
                                "seatId": 203,
                                "seatName": "A3",
                                "rowCode": "A",
                                "seatNumber": 3,
                                "seatTypeName": "Standard",
                                "price": 80000,
                                "status": "SOLD"
                              }
                            ],
                            "serviceOrders": [
                              {
                                "orderId": 301,
                                "serviceId": 10,
                                "serviceName": "Combo Bắp Nước Lớn",
                                "description": "1 Bắp lớn + 2 Nước ngọt 32oz",
                                "quantity": 2,
                                "unitPrice": 90000,
                                "totalPrice": 180000
                              },
                              {
                                "orderId": 302,
                                "serviceId": 15,
                                "serviceName": "Hotdog",
                                "description": "Hotdog kèm sốt",
                                "quantity": 1,
                                "unitPrice": 40000,
                                "totalPrice": 40000
                              }
                            ],
                            "payment": {
                              "paymentId": 601,
                              "amount": 230000,
                              "method": "QR_CODE",
                              "status": "PAID",
                              "provider": "PAYOS",
                              "transactionId": "payos_tx_789456123",
                              "paidAt": "2025-11-16T20:15:05",
                              "signatureOk": true
                            },
                            "voucher": {
                              "voucherId": 25,
                              "voucherCode": "BLACKFRIDAY2025",
                              "discountType": "percent",
                              "discountVal": 15.0,
                              "validFrom": "2025-11-01T00:00:00",
                              "validTo": "2025-11-30T23:59:59"
                            },
                            "pricingBreakdown": {
                              "ticketsSubtotal": 280000,
                              "servicesSubtotal": 220000,
                              "subtotalBeforeVoucher": 500000,
                              "voucherDiscount": 75000,
                              "finalTotal": 425000,
                              "commissionAmount": 42500,
                              "commissionRate": 10.0
                            }
                          }
                        }
                        """
                        )
                    });

                    // Use Case 2: Simple booking without voucher and services
                    content.Examples.Add("Use Case 2 - Simple Booking", new OpenApiExample
                    {
                        Summary = "Đơn hàng đơn giản chỉ có vé, không có voucher/combo",
                        Description = "GET /api/manager/bookings/456",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 456,
                              "bookingCode": "BK20251117XYZ456",
                              "bookingTime": "2025-11-17T15:30:00",
                              "totalAmount": 160000,
                              "status": "PAID",
                              "state": "COMPLETED",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": "payos_tx_123789456",
                              "paymentStatus": "PAID",
                              "orderCode": "ORD20251117002",
                              "sessionId": "660e8400-e29b-41d4-a716-446655440111",
                              "createdAt": "2025-11-17T15:29:00",
                              "updatedAt": "2025-11-17T15:30:30"
                            },
                            "customer": {
                              "customerId": 52,
                              "userId": 102,
                              "fullname": "Trần Thị B",
                              "email": "tranthib@example.com",
                              "phone": "0923456789",
                              "username": "tranthib"
                            },
                            "showtime": {
                              "showtimeId": 1005,
                              "showDatetime": "2025-11-21T21:00:00",
                              "endTime": "2025-11-21T23:45:00",
                              "basePrice": 80000,
                              "status": "ACTIVE",
                              "formatType": "2D",
                              "availableSeats": 92
                            },
                            "cinema": {
                              "cinemaId": 8,
                              "cinemaName": "Galaxy Nguyễn Du",
                              "address": "116 Nguyễn Du, Phường Bến Thành",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Quận 1",
                              "code": "GLX-ND-01",
                              "isActive": true
                            },
                            "screen": {
                              "screenId": 205,
                              "screenName": "Phòng 5",
                              "code": "P05",
                              "capacity": 100,
                              "screenType": "Premium",
                              "soundSystem": "Dolby 7.1"
                            },
                            "partner": {
                              "partnerId": 2,
                              "partnerName": "Galaxy Cinema",
                              "taxCode": "9876543210",
                              "status": "approved",
                              "commissionRate": 8.5
                            },
                            "movie": {
                              "movieId": 15,
                              "title": "Dune: Part Three",
                              "genre": "Sci-Fi, Adventure, Drama",
                              "durationMinutes": 165,
                              "director": "Denis Villeneuve",
                              "language": "English",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/dune3.jpg",
                              "premiereDate": "2025-11-10",
                              "endDate": "2025-12-15"
                            },
                            "tickets": [
                              {
                                "ticketId": 601,
                                "seatId": 301,
                                "seatName": "C5",
                                "rowCode": "C",
                                "seatNumber": 5,
                                "seatTypeName": "Standard",
                                "price": 80000,
                                "status": "SOLD"
                              },
                              {
                                "ticketId": 602,
                                "seatId": 302,
                                "seatName": "C6",
                                "rowCode": "C",
                                "seatNumber": 6,
                                "seatTypeName": "Standard",
                                "price": 80000,
                                "status": "SOLD"
                              }
                            ],
                            "serviceOrders": [],
                            "payment": {
                              "paymentId": 701,
                              "amount": 160000,
                              "method": "QR_CODE",
                              "status": "PAID",
                              "provider": "PAYOS",
                              "transactionId": "payos_tx_123789456",
                              "paidAt": "2025-11-17T15:30:15",
                              "signatureOk": true
                            },
                            "voucher": null,
                            "pricingBreakdown": {
                              "ticketsSubtotal": 160000,
                              "servicesSubtotal": 0,
                              "subtotalBeforeVoucher": 160000,
                              "voucherDiscount": 0,
                              "finalTotal": 160000,
                              "commissionAmount": 13600,
                              "commissionRate": 8.5
                            }
                          }
                        }
                        """
                        )
                    });

                    // Use Case 3: Booking with fixed discount voucher
                    content.Examples.Add("Use Case 3 - Fixed Voucher", new OpenApiExample
                    {
                        Summary = "Đơn hàng có voucher giảm giá cố định",
                        Description = "GET /api/manager/bookings/789",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy chi tiết đơn hàng thành công.",
                          "result": {
                            "booking": {
                              "bookingId": 789,
                              "bookingCode": "BK20251118DEF789",
                              "bookingTime": "2025-11-18T10:00:00",
                              "totalAmount": 290000,
                              "status": "PAID",
                              "state": "COMPLETED",
                              "paymentProvider": "PAYOS",
                              "paymentTxId": "payos_tx_456123789",
                              "paymentStatus": "PAID",
                              "orderCode": "ORD20251118003",
                              "sessionId": "770e8400-e29b-41d4-a716-446655440222",
                              "createdAt": "2025-11-18T09:59:00",
                              "updatedAt": "2025-11-18T10:00:45"
                            },
                            "customer": {
                              "customerId": 75,
                              "userId": 135,
                              "fullname": "Phạm Minh C",
                              "email": "phamminhc@example.com",
                              "phone": "0945678901",
                              "username": "phamminhc"
                            },
                            "showtime": {
                              "showtimeId": 1020,
                              "showDatetime": "2025-11-22T14:00:00",
                              "endTime": "2025-11-22T16:30:00",
                              "basePrice": 90000,
                              "status": "ACTIVE",
                              "formatType": "3D",
                              "availableSeats": 75
                            },
                            "cinema": {
                              "cinemaId": 12,
                              "cinemaName": "BHD Star Vincom",
                              "address": "72 Lê Thánh Tôn, Phường Bến Nghé",
                              "city": "TP. Hồ Chí Minh",
                              "district": "Quận 1",
                              "code": "BHD-VCM-01",
                              "isActive": true
                            },
                            "screen": {
                              "screenId": 310,
                              "screenName": "Phòng 3",
                              "code": "P03",
                              "capacity": 90,
                              "screenType": "3D",
                              "soundSystem": "DTS:X"
                            },
                            "partner": {
                              "partnerId": 3,
                              "partnerName": "BHD Star Cineplex",
                              "taxCode": "5555555555",
                              "status": "approved",
                              "commissionRate": 12.0
                            },
                            "movie": {
                              "movieId": 25,
                              "title": "Spider-Man: Beyond Dimensions",
                              "genre": "Action, Adventure, Animation",
                              "durationMinutes": 140,
                              "director": "Joaquim Dos Santos",
                              "language": "English",
                              "country": "USA",
                              "posterUrl": "https://example.com/posters/spiderman.jpg",
                              "premiereDate": "2025-11-18",
                              "endDate": "2025-12-25"
                            },
                            "tickets": [
                              {
                                "ticketId": 701,
                                "seatId": 401,
                                "seatName": "D8",
                                "rowCode": "D",
                                "seatNumber": 8,
                                "seatTypeName": "VIP",
                                "price": 120000,
                                "status": "SOLD"
                              },
                              {
                                "ticketId": 702,
                                "seatId": 402,
                                "seatName": "D9",
                                "rowCode": "D",
                                "seatNumber": 9,
                                "seatTypeName": "VIP",
                                "price": 120000,
                                "status": "SOLD"
                              }
                            ],
                            "serviceOrders": [
                              {
                                "orderId": 401,
                                "serviceId": 12,
                                "serviceName": "Combo Couple",
                                "description": "2 Bắp lớn + 2 Nước ngọt 32oz + 1 Snack",
                                "quantity": 1,
                                "unitPrice": 100000,
                                "totalPrice": 100000
                              }
                            ],
                            "payment": {
                              "paymentId": 801,
                              "amount": 290000,
                              "method": "QR_CODE",
                              "status": "PAID",
                              "provider": "PAYOS",
                              "transactionId": "payos_tx_456123789",
                              "paidAt": "2025-11-18T10:00:30",
                              "signatureOk": true
                            },
                            "voucher": {
                              "voucherId": 30,
                              "voucherCode": "WELCOME50K",
                              "discountType": "fixed",
                              "discountVal": 50000,
                              "validFrom": "2025-11-01T00:00:00",
                              "validTo": "2025-12-31T23:59:59"
                            },
                            "pricingBreakdown": {
                              "ticketsSubtotal": 240000,
                              "servicesSubtotal": 100000,
                              "subtotalBeforeVoucher": 340000,
                              "voucherDiscount": 50000,
                              "finalTotal": 290000,
                              "commissionAmount": 34800,
                              "commissionRate": 12.0
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
                    content.Examples.Add("Not Manager", new OpenApiExample
                    {
                        Summary = "Không phải Manager",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Bạn không có quyền truy cập. Chỉ Manager mới có thể xem chi tiết đơn hàng.",
                          "errors": {
                            "auth": {
                              "msg": "Bạn không có quyền truy cập. Chỉ Manager mới có thể xem chi tiết đơn hàng.",
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
                    content.Examples.Add("Booking Not Found", new OpenApiExample
                    {
                        Summary = "Không tìm thấy đơn hàng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy đơn hàng với ID 999."
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

