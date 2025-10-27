using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example.Partner
{
    public class SeatTypeExamplesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            // Xử lý cho GetSeatTypes
            if (controllerName == "Partners" && actionName == "GetSeatTypes")
            {
                ApplyGetSeatTypesExamples(operation);
            }
            // Xử lý cho GetSeatTypeById
            else if (controllerName == "Partners" && actionName == "GetSeatTypeById")
            {
                ApplyGetSeatTypeByIdExamples(operation);
            }
            // Xử lý cho CreateSeatType
            else if (controllerName == "Partners" && actionName == "CreateSeatType")
            {
                ApplyCreateSeatTypeExamples(operation);
            }
            // Xử lý cho UpdateSeatType
            else if (controllerName == "Partners" && actionName == "UpdateSeatType")
            {
                ApplyUpdateSeatTypeExamples(operation);
            }
            // Xử lý cho DeleteSeatType
            else if (controllerName == "Partners" && actionName == "DeleteSeatType")
            {
                ApplyDeleteSeatTypeExamples(operation);
            }
        }

        private void ApplyGetSeatTypesExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy danh sách loại ghế thành công",
                          "result": {
                            "seatTypes": [
                              {
                                "id": 1,
                                "code": "STANDARD",
                                "name": "Ghế Thường",
                                "surcharge": 0,
                                "color": "#1e90ff",
                                "description": "Ghế ngồi tiêu chuẩn",
                                "status": true,
                                "createdAt": "2025-10-18T18:06:25.403Z",
                                "updatedAt": "2025-10-21T20:42:42.573Z"
                              },
                              {
                                "id": 2,
                                "code": "VIP",
                                "name": "Ghế VIP",
                                "surcharge": 50000,
                                "color": "#ffd700",
                                "description": "Ghế VIP thoải mái với không gian rộng rãi",
                                "status": true,
                                "createdAt": "2025-10-18T18:06:25.403Z",
                                "updatedAt": "2025-10-20T20:40:00.947Z"
                              }
                            ],
                            "pagination": {
                              "currentPage": 1,
                              "pageSize": 10,
                              "totalCount": 15,
                              "totalPages": 2
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Invalid Pagination", new OpenApiExample
                    {
                        Summary = "Pagination không hợp lệ",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "page": {
                              "msg": "Số trang phải lớn hơn 0",
                              "path": "page"
                            },
                            "limit": {
                              "msg": "Số lượng mỗi trang phải từ 1 đến 100",
                              "path": "limit"
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
                    content.Examples.Add("Partner Not Approved", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "partner": {
                              "msg": "Partner không tồn tại hoặc chưa được phê duyệt",
                              "path": "partner"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 404 Not Found - BỔ SUNG
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("No SeatTypes Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy loại ghế nào với bộ lọc hiện tại"
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
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi lấy danh sách loại ghế."
                        }
                        """
                        )
                    });
                }
            }
        }

        private void ApplyGetSeatTypeByIdExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lấy thông tin loại ghế thành công",
                          "result": {
                            "id": 2,
                            "code": "VIP",
                            "name": "Ghế VIP",
                            "surcharge": 50000,
                            "color": "#ffd700",
                            "description": "Ghế VIP thoải mái với không gian rộng rãi",
                            "status": true,
                            "createdAt": "2025-10-18T18:06:25.403Z",
                            "updatedAt": "2025-10-20T20:40:00.947Z",
                            "totalSeats": 45,
                            "activeSeats": 40,
                            "inactiveSeats": 5
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Invalid ID", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "id": {
                              "msg": "ID loại ghế phải lớn hơn 0",
                              "path": "id"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 401 Unauthorized - BỔ SUNG
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Unauthorized Access", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "user": {
                              "msg": "Chỉ tài khoản Partner mới được sử dụng chức năng này",
                              "path": "user"
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
                    content.Examples.Add("SeatType Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn"
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error - BỔ SUNG
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi lấy thông tin loại ghế."
                        }
                        """
                        )
                    });
                }
            }
        }

        private void ApplyCreateSeatTypeExamples(OpenApiOperation operation)
        {
            // Request Body Example
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "code": "VIP_PREMIUM",
                          "name": "Ghế VIP Premium",
                          "surcharge": 150000,
                          "color": "#FFD700",
                          "description": "Ghế VIP cao cấp với không gian thoải mái"
                        }
                        """
                        )
                    }
                }
            };

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Tạo loại ghế thành công",
                          "result": {
                            "id": 6,
                            "code": "VIP_PREMIUM",
                            "name": "Ghế VIP Premium",
                            "surcharge": 150000,
                            "color": "#FFD700",
                            "description": "Ghế VIP cao cấp với không gian thoải mái",
                            "status": true,
                            "createdAt": "2025-10-22T10:30:00.000Z",
                            "updatedAt": "2025-10-22T10:30:00.000Z",
                            "message": "Tạo loại ghế thành công"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Validation Errors", new OpenApiExample
                    {
                        Summary = "Lỗi validation",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "code": {
                              "msg": "Mã loại ghế là bắt buộc",
                              "path": "code"
                            },
                            "name": {
                              "msg": "Tên loại ghế là bắt buộc",
                              "path": "name"
                            },
                            "color": {
                              "msg": "Màu sắc phải là mã hex hợp lệ",
                              "path": "color"
                            },
                            "surcharge": {
                              "msg": "Phụ thu không được vượt quá 1,000,000",
                              "path": "surcharge"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 401 Unauthorized - BỔ SUNG
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Partner Not Active", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "partner": {
                              "msg": "Tài khoản partner đã bị vô hiệu hóa",
                              "path": "partner"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 409 Conflict
            if (operation.Responses.ContainsKey("409"))
            {
                var response = operation.Responses["409"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Duplicate Code", new OpenApiExample
                    {
                        Summary = "Mã loại ghế đã tồn tại",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Dữ liệu bị xung đột",
                          "errors": {
                            "code": {
                              "msg": "Mã loại ghế đã tồn tại trong hệ thống của bạn",
                              "path": "code"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error - BỔ SUNG
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi tạo loại ghế."
                        }
                        """
                        )
                    });
                }
            }
        }

        private void ApplyUpdateSeatTypeExamples(OpenApiOperation operation)
        {
            // Request Body Example
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString(
                        """
                        {
                          "name": "Ghế VIP Premium Updated",
                          "surcharge": 160000,
                          "color": "#FF6B35",
                          "description": "Ghế VIP cao cấp cập nhật mới",
                          "status": true
                        }
                        """
                        )
                    }
                }
            };

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Cập nhật loại ghế thành công",
                          "result": {
                            "id": 2,
                            "code": "VIP",
                            "name": "Ghế VIP Premium Updated",
                            "surcharge": 160000,
                            "color": "#FF6B35",
                            "description": "Ghế VIP cao cấp cập nhật mới",
                            "status": true,
                            "createdAt": "2025-10-18T18:06:25.403Z",
                            "updatedAt": "2025-10-22T11:00:00.000Z",
                            "message": "Cập nhật loại ghế thành công"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Cannot Disable", new OpenApiExample
                    {
                        Summary = "Không thể vô hiệu hóa khi có ghế đang sử dụng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "status": {
                              "msg": "Không thể vô hiệu hóa loại ghế đang có 25 ghế đang hoạt động",
                              "path": "status"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 401 Unauthorized - BỔ SUNG
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Unauthorized Access", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "partner": {
                              "msg": "Partner không tồn tại hoặc không thuộc quyền quản lý của bạn",
                              "path": "partner"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 404 Not Found - BỔ SUNG
            if (operation.Responses.ContainsKey("404"))
            {
                var response = operation.Responses["404"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("SeatType Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn"
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error - BỔ SUNG
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi cập nhật loại ghế."
                        }
                        """
                        )
                    });
                }
            }
        }

        private void ApplyDeleteSeatTypeExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xóa loại ghế thành công",
                          "result": {
                            "id": 5,
                            "code": "VIP_PREMIUM",
                            "name": "Ghế VIP Premium",
                            "surcharge": 150000,
                            "color": "#a11212",
                            "description": "Ghế vip góc nhìn tuyệt vời",
                            "status": false,
                            "createdAt": "2025-10-18T18:31:35.990Z",
                            "updatedAt": "2025-10-22T11:30:00.000Z",
                            "message": "Xóa loại ghế thành công"
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 400 Bad Request
            if (operation.Responses.ContainsKey("400"))
            {
                var response = operation.Responses["400"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Cannot Delete", new OpenApiExample
                    {
                        Summary = "Không thể xóa khi có ghế đang sử dụng",
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Lỗi xác thực dữ liệu",
                          "errors": {
                            "delete": {
                              "msg": "Không thể xóa loại ghế đang có 15 ghế sử dụng",
                              "path": "id"
                            }
                          }
                        }
                        """
                        )
                    });
                }
            }

            // Response 401 Unauthorized - BỔ SUNG
            if (operation.Responses.ContainsKey("401"))
            {
                var response = operation.Responses["401"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Unauthorized Access", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Xác thực thất bại",
                          "errors": {
                            "user": {
                              "msg": "Chỉ tài khoản Partner mới được sử dụng chức năng này",
                              "path": "user"
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
                    content.Examples.Add("SeatType Not Found", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Không tìm thấy loại ghế với ID này hoặc không thuộc quyền quản lý của bạn"
                        }
                        """
                        )
                    });
                }
            }

            // Response 500 Internal Server Error - BỔ SUNG
            if (operation.Responses.ContainsKey("500"))
            {
                var response = operation.Responses["500"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Server Error", new OpenApiExample
                    {
                        Value = new OpenApiString(
                        """
                        {
                          "message": "Đã xảy ra lỗi hệ thống khi xóa loại ghế."
                        }
                        """
                        )
                    });
                }
            }

            // Thêm description cho parameter
            if (operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters)
                {
                    if (parameter.Name == "id")
                    {
                        parameter.Description = "Seat Type ID";
                        parameter.Required = true;
                        parameter.Example = new OpenApiInteger(1);
                    }
                }
            }
        }
    }
}