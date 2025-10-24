using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Example
{
    public class AdminUserExamplesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            if (controllerName != "Admin") return;

            var method = context.ApiDescription.HttpMethod?.ToUpper();
            var path = context.ApiDescription.RelativePath;

            if (method == "GET" && path == "api/admin/users")
            {
                ApplyGetAllUsersExamples(operation);
            }
            else if (method == "DELETE" && path?.StartsWith("api/admin/users/") == true)
            {
                ApplyDeleteUserExamples(operation);
            }
            else if (method == "PUT" && path?.Contains("/ban") == true)
            {
                ApplyBanUserExamples(operation);
            }
            else if (method == "PUT" && path?.Contains("/unban") == true)
            {
                ApplyUnbanUserExamples(operation);
            }
            else if (method == "PUT" && path?.Contains("/role") == true)
            {
                ApplyUpdateUserRoleExamples(operation);
            }
            else if (method == "PUT" && path?.StartsWith("api/admin/users/") == true &&
                     !path.Contains("/ban") && !path.Contains("/unban") && !path.Contains("/role"))
            {
                ApplyUpdateUserExamples(operation);
            }
            else if (method == "GET" && path?.StartsWith("api/admin/users/") == true &&
                     !path.Contains("/ban") && !path.Contains("/unban") && !path.Contains("/role"))
            {
                // GET /api/admin/users/{id} - Get user by ID
                ApplyGetUserByIdExamples(operation);
            }
        }

        private void ApplyUpdateUserExamples(OpenApiOperation operation)
        {
            // Request body example
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Request Example", new OpenApiExample
                    {
                        Summary = "Update user request",
                        Value = new OpenApiString(
                            """
                    {
                        "email": "updated.user@example.com",
                        "phone": "+1234567890",
                        "userType": "User",
                        "fullname": "Updated User Name",
                        "isActive": true,
                        "emailConfirmed": true,
                        "username": "updatedusername",
                        "avataUrl": "https://example.com/avatar.jpg",
                        "isBanned": false
                    }
                    """
                        )
                    });
                }
            }

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User updated successfully",
                        Value = new OpenApiString(
                            """
                    {
                        "message": "Cập nhật người dùng thành công",
                        "result": {
                            "userId": "123",
                            "email": "updated.user@example.com",
                            "phone": "+1234567890",
                            "userType": "User",
                            "fullname": "Updated User Name",
                            "isActive": true,
                            "createdAt": "2024-01-15T10:30:00Z",
                            "emailConfirmed": true,
                            "username": "updatedusername",
                            "avataUrl": "https://example.com/avatar.jpg",
                            "updatedAt": "2024-01-25T18:30:00Z",
                            "isBanned": false,
                            "bannedAt": null,
                            "unbannedAt": null,
                            "deactivatedAt": null
                        }
                    }
                    """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid User Id",
                """
        {
            "message": "ID người dùng không hợp lệ",
            "errorInfo": {
                "name": "ValidationError",
                "message": "ID người dùng phải là số hợp lệ"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "Không tìm thấy người dùng",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "Không tìm thấy người dùng"
            }
        }
        """);

            AddErrorResponseExamples(operation, "409", "Conflict - Username already exists",
                """
        {
            "message": "Tên người dùng đã tồn tại",
            "errorInfo": {
                "name": "ConflictError",
                "message": "Tên người dùng đã tồn tại"
            }
        }
        """);

            AddErrorResponseExamples(operation, "409", "Conflict - Email already exists",
                """
        {
            "message": "Email đã tồn tại",
            "errorInfo": {
                "name": "ConflictError",
                "message": "Email đã tồn tại"
            }
        }
        """);

            AddErrorResponseExamples(operation, "409", "Conflict - Multiple fields exist",
                """
        {
            "message": "Tên người dùng và email đã tồn tại",
            "errorInfo": {
                "name": "ConflictError",
                "message": "Tên người dùng và email đã tồn tại"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "Đã xảy ra lỗi máy chủ nội bộ",
            "errorInfo": {
                "name": "ServerError",
                "message": "Internal server error details"
            }
        }
        """);
        }

        private void ApplyGetUserByIdExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User retrieved successfully",
                        Value = new OpenApiString(
                            """
                    {
                        "message": "Lấy thông tin người dùng thành công",
                        "result": {
                            "userId": 123,
                            "email": "john.doe@example.com",
                            "phone": "+1234567890",
                            "userType": "User",
                            "fullname": "John Doe",
                            "isActive": true,
                            "createdAt": "2024-01-15T10:30:00Z",
                            "emailConfirmed": true,
                            "username": "johndoe",
                            "avataUrl": "https://example.com/avatar.jpg",
                            "updatedAt": "2024-01-20T14:30:00Z",
                            "isBanned": false,
                            "bannedAt": null,
                            "unbannedAt": null,
                            "deactivatedAt": null
                        }
                    }
                    """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid User Id",
                """
        {
            "message": "ID người dùng không hợp lệ",
            "errorInfo": {
                "name": "ValidationError",
                "message": "ID người dùng phải là số hợp lệ"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "Không tìm thấy người dùng",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "Không tìm thấy người dùng"
            }
        }
        """);

            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
        {
            "message": "Truy cập không được ủy quyền",
            "errorInfo": {
                "name": "AuthenticationError",
                "message": "Người dùng chưa được xác thực"
            }
        }
        """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
        {
            "message": "Truy cập bị cấm",
            "errorInfo": {
                "name": "AuthorizationError",
                "message": "Người dùng không có vai trò Quản trị viên"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "Đã xảy ra lỗi máy chủ nội bộ",
            "errorInfo": {
                "name": "ServerError",
                "message": "Internal server error details"
            }
        }
        """);
        }

        private void ApplyUpdateUserRoleExamples(OpenApiOperation operation)
        {
            // Request body example
            if (operation.RequestBody != null)
            {
                var content = operation.RequestBody.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Request Example", new OpenApiExample
                    {
                        Summary = "Update user role request",
                        Value = new OpenApiString(
                            """
                    {
                        "role": "User"
                    }
                    """
                        )
                    });
                }
            }

            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User role updated successfully",
                        Value = new OpenApiString(
                            """
                    {
                        "message": "Cập nhật vai trò người dùng thành công",
                        "result": {
                            "userId": "123",
                            "role": "user",
                            "updatedAt": "2024-01-25T17:30:00Z"
                        }
                    }
                    """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
        {
            "message": "Truy cập không được ủy quyền",
            "errorInfo": {
                "name": "AuthenticationError",
                "message": "Người dùng chưa được xác thực"
            }
        }
        """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
        {
            "message": "Truy cập bị cấm",
            "errorInfo": {
                "name": "AuthorizationError",
                "message": "Người dùng không có vai trò Quản trị viên"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid role",
                """
        {
            "message": "Vai trò không hợp lệ. Các giá trị có sẵn: user, staff, partner, manager, admin",
            "errorInfo": {
                "name": "ValidationError",
                "message": "Tham số vai trò không hợp lệ"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Role is required",
                """
        {
            "message": "Vai trò là bắt buộc",
            "errorInfo": {
                "name": "ValidationError",
                "message": "Trường vai trò không được để trống"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "Không tìm thấy người dùng",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "Không tìm thấy người dùng"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "Đã xảy ra lỗi máy chủ nội bộ",
            "errorInfo": {
                "name": "ServerError",
                "message": "Internal server error details"
            }
        }
        """);
        }

        private void ApplyUnbanUserExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User unbanned successfully",
                        Value = new OpenApiString(
                            """
                    {
                        "message": "Bỏ cấm người dùng thành công",
                        "result": {
                            "userId": "123",
                            "isBanned": false,
                            "unbannedAt": "2024-01-25T16:30:00Z"
                        }
                    }
                    """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
        {
            "message": "Truy cập không được ủy quyền",
            "errorInfo": {
                "name": "AuthenticationError",
                "message": "Người dùng chưa được xác thực"
            }
        }
        """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
        {
            "message": "Truy cập bị cấm",
            "errorInfo": {
                "name": "AuthorizationError",
                "message": "Người dùng không có vai trò Quản trị viên"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - User is not banned",
                """
        {
            "message": "Người dùng không bị cấm",
            "errorInfo": {
                "name": "BusinessRuleError",
                "message": "Người dùng không bị cấm"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "Không tìm thấy người dùng",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "Không tìm thấy người dùng"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "Đã xảy ra lỗi máy chủ nội bộ",
            "errorInfo": {
                "name": "ServerError",
                "message": "Internal server error details"
            }
        }
        """);
        }

        private void ApplyBanUserExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User banned successfully",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Cấm người dùng thành công",
                                "result": {
                                    "userId": "123",
                                    "isBanned": true,
                                    "bannedAt": "2024-01-25T15:30:00Z"
                                }
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
                {
                    "message": "Truy cập không được ủy quyền",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "Người dùng chưa được xác thực"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Truy cập bị cấm",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "Người dùng không có vai trò Quản trị viên"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - User already banned",
                """
                {
                    "message": "Người dùng đã bị cấm",
                    "errorInfo": {
                        "name": "BusinessRuleError",
                        "message": "Người dùng đã bị cấm"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
                {
                    "message": "Không tìm thấy người dùng",
                    "errorInfo": {
                        "name": "NotFoundError",
                        "message": "Không tìm thấy người dùng"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "Đã xảy ra lỗi máy chủ nội bộ",
                    "errorInfo": {
                        "name": "ServerError",
                        "message": "Internal server error details"
                    }
                }
                """);
        }

        private void ApplyGetAllUsersExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "Successful response with user list",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Lấy danh sách người dùng thành công",
                                "result": {
                                    "users": [
                                        {
                                            "id": 1,
                                            "name": "John Doe",
                                            "email": "john.doe@example.com",
                                            "username": "johndoe",
                                            "role": "user",
                                            "verify": 1,
                                            "createdAt": "2024-01-15T10:30:00Z",
                                            "stats": {
                                                "bookingsCount": 5,
                                                "ratingsCount": 3,
                                                "commentsCount": 2
                                            }
                                        }
                                    ],
                                    "page": 1,
                                    "limit": 10,
                                    "total": 1,
                                    "totalPages": 1
                                }
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
                {
                    "message": "Truy cập không được ủy quyền",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "Người dùng chưa được xác thực"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Truy cập bị cấm",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "Người dùng không có vai trò Quản trị viên"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request",
                """
                {
                    "message": "Giá trị vai trò không hợp lệ. Các giá trị có sẵn: user, staff, admin, manager, partner",
                    "errorInfo": {
                        "name": "ValidationError",
                        "message": "Tham số vai trò không hợp lệ"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "Đã xảy ra lỗi máy chủ nội bộ.",
                    "errorInfo": {
                        "name": "ServerError",
                        "message": "Internal server error details"
                    }
                }
                """);
        }

        private void ApplyDeleteUserExamples(OpenApiOperation operation)
        {
            // Response 200 OK
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add("Success Example", new OpenApiExample
                    {
                        Summary = "User deleted successfully",
                        Value = new OpenApiString(
                            """
                            {
                                "message": "Xóa người dùng thành công",
                                "result": {
                                    "userId": "123",
                                    "deleted": true,
                                    "deactivatedAt": "2024-01-25T15:30:00Z"
                                }
                            }
                            """
                        )
                    });
                }
            }

            // Error response examples
            AddErrorResponseExamples(operation, "401", "Unauthorized",
                """
                {
                    "message": "Truy cập không được ủy quyền",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "Người dùng chưa được xác thực"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Truy cập bị cấm",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "Người dùng không có vai trò Quản trị viên"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request",
                """
                {
                    "message": "Định dạng ID người dùng không hợp lệ",
                    "errorInfo": {
                        "name": "ValidationError",
                        "message": "ID người dùng phải là số hợp lệ"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
                {
                    "message": "Không tìm thấy người dùng",
                    "errorInfo": {
                        "name": "NotFoundError",
                        "message": "Không tìm thấy người dùng"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "Đã xảy ra lỗi máy chủ nội bộ",
                    "errorInfo": {
                        "name": "ServerError",
                        "message": "Internal server error details"
                    }
                }
                """);
        }

        private void AddErrorResponseExamples(OpenApiOperation operation, string statusCode, string summary, string exampleJson)
        {
            if (operation.Responses.ContainsKey(statusCode))
            {
                var response = operation.Responses[statusCode];
                var content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value;
                if (content != null)
                {
                    content.Examples.Clear();
                    content.Examples.Add($"{statusCode} Example", new OpenApiExample
                    {
                        Summary = summary,
                        Value = new OpenApiString(exampleJson)
                    });
                }
            }
        }
    }
}