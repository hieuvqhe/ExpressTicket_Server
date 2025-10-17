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
            else if (method == "PUT" && path?.Contains("/role") == true) // THÊM UPDATE ROLE
            {
                ApplyUpdateUserRoleExamples(operation);
            }
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
                        "role": "customer"
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
                        "message": "Update user role success",
                        "result": {
                            "userId": "123",
                            "role": "customer",
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
            "message": "Unauthorized access",
            "errorInfo": {
                "name": "AuthenticationError",
                "message": "User is not authenticated"
            }
        }
        """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
        {
            "message": "Access forbidden",
            "errorInfo": {
                "name": "AuthorizationError",
                "message": "User does not have Admin role"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Invalid role",
                """
        {
            "message": "Invalid role. Available values: customer, employee, partner, manager, admin",
            "errorInfo": {
                "name": "ValidationError",
                "message": "Invalid role parameter"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - Role is required",
                """
        {
            "message": "Role is required",
            "errorInfo": {
                "name": "ValidationError",
                "message": "Role field cannot be empty"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "User not found",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "User not found"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "An internal server error occurred",
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
                        "message": "Unban user success",
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
            "message": "Unauthorized access",
            "errorInfo": {
                "name": "AuthenticationError",
                "message": "User is not authenticated"
            }
        }
        """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
        {
            "message": "Access forbidden",
            "errorInfo": {
                "name": "AuthorizationError",
                "message": "User does not have Admin role"
            }
        }
        """);

            AddErrorResponseExamples(operation, "400", "Bad Request - User is not banned",
                """
        {
            "message": "User is not banned",
            "errorInfo": {
                "name": "BusinessRuleError",
                "message": "User is not banned"
            }
        }
        """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
        {
            "message": "User not found",
            "errorInfo": {
                "name": "NotFoundError",
                "message": "User not found"
            }
        }
        """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
        {
            "message": "An internal server error occurred",
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
                                "message": "Ban user success",
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
                    "message": "Unauthorized access",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "User is not authenticated"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Access forbidden",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "User does not have Admin role"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request - User already banned",
                """
                {
                    "message": "User already banned",
                    "errorInfo": {
                        "name": "BusinessRuleError",
                        "message": "User already banned"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
                {
                    "message": "User not found",
                    "errorInfo": {
                        "name": "NotFoundError",
                        "message": "User not found"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "An internal server error occurred",
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
                                "message": "Get users success",
                                "result": {
                                    "users": [
                                        {
                                            "id": 1,
                                            "name": "John Doe",
                                            "email": "john.doe@example.com",
                                            "username": "johndoe",
                                            "role": "customer",
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
                    "message": "Unauthorized access",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "User is not authenticated"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Access forbidden",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "User does not have Admin role"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request",
                """
                {
                    "message": "Invalid role value. Available values: customer, staff, admin",
                    "errorInfo": {
                        "name": "ValidationError",
                        "message": "Invalid role parameter"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "An internal server error occurred.",
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
                                "message": "Delete user success",
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
                    "message": "Unauthorized access",
                    "errorInfo": {
                        "name": "AuthenticationError",
                        "message": "User is not authenticated"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "403", "Forbidden",
                """
                {
                    "message": "Access forbidden",
                    "errorInfo": {
                        "name": "AuthorizationError",
                        "message": "User does not have Admin role"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "400", "Bad Request",
                """
                {
                    "message": "Invalid user ID format",
                    "errorInfo": {
                        "name": "ValidationError",
                        "message": "User ID must be a valid number"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "404", "Not Found",
                """
                {
                    "message": "User not found",
                    "errorInfo": {
                        "name": "NotFoundError",
                        "message": "User not found"
                    }
                }
                """);

            AddErrorResponseExamples(operation, "500", "Internal Server Error",
                """
                {
                    "message": "An internal server error occurred",
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