    using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
    using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;
    using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses;
    using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using static ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum.AdminEnum;

    namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
    {
        [ApiController]
        [Route("api/admin")]
        [Produces("application/json")]
        public class AdminController : ControllerBase
        {
            private readonly AdminService _adminService;

            public AdminController(AdminService adminService)
            {
                _adminService = adminService;
            }

            /// <summary>
            /// Get all users 
            /// </summary>
            /// <remarks>
            /// Admin only - Retrieve all users with optional filters and pagination
            /// </remarks>
            /// <param name="page">Page number (Default value: 1)</param>
            /// <param name="limit">Number of items per page (Default value: 10)</param>
            /// <param name="search">Search by name, email, or username</param>
            /// <param name="role">Filter by user role (Available values: customer, employee, admin, partner, manager)</param>
            /// <param name="verify">Filter by verification status (0=unverified, 1=verified, 2=banned)</param>
            /// <param name="sortBy">Field to sort by (Default value: created_at)</param>
            /// <param name="sortOrder">Sort order (Available values: asc, desc, Default value: desc)</param>
            [HttpGet("users")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(AdminPaginatedUserListResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> GetAllUsers(
                [FromQuery] int page = 1,
                [FromQuery] int limit = 10,
                [FromQuery] string? search = null,
                [FromQuery] string? role = null,
                [FromQuery] VerifyStatus? verify = null,
                [FromQuery(Name = "sort_by")] string sortBy = "created_at",
                [FromQuery(Name = "sort_order")] string sortOrder = "desc")
            {
                try
                {
                    // Validate role parameter - không phân biệt chữ hoa/chữ thường
                    if (!string.IsNullOrEmpty(role))
                    {
                        var validRoles = new List<string> { "customer", "employee", "admin", "partner", "manager" };
                        var normalizedRole = role.ToLower();

                        if (!validRoles.Contains(normalizedRole))
                        {
                            return BadRequest(new ErrorAdminResponse
                            {
                                Message = "Invalid role value. Available values: customer, employee, admin, partner, manager",
                                ErrorInfo = new ErrorInfo
                                {
                                    Name = "ValidationError",
                                    Message = "Invalid role parameter"
                                }
                            });
                        }

                        // Gán lại giá trị role đã được chuẩn hóa
                        role = normalizedRole;
                    }

                    (List<User> users, int total) = await _adminService.GetFilteredUsersAsync(
                        page, limit, search, role, verify?.ToString(), sortBy, sortOrder
                    );

                    var userResponses = new List<AdminUserInfoResponse>();
                    foreach (var user in users)
                    {
                        var stats = await _adminService.GetUserStatsAsync(user.UserId);
                        var userResponse = new AdminUserInfoResponse
                        {
                            Id = user.UserId,
                            Name = user.Fullname,
                            Email = user.Email,
                            Username = user.Username,
                            Role = user.UserType.ToLower(),
                            Verify = user.IsBanned ? 2 : (!user.EmailConfirmed ? 0 : 1),
                            CreatedAt = user.CreatedAt,
                            Stats = stats
                        };
                        userResponses.Add(userResponse);
                    }

                    var response = new AdminPaginatedUserListResponse
                    {
                        Message = "Get users success",
                        Result = new AdminPaginatedUserResponse
                        {
                            Users = userResponses,
                            Page = page,
                            Limit = limit,
                            Total = total,
                            TotalPages = (int)Math.Ceiling((double)total / limit)
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred.",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Delete user by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Deactivate user 
            /// </remarks>
            [HttpDelete("users/{user_id}")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(DeleteUserResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> DeleteUser(string user_id)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID format",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Lấy thông tin user hiện tại (admin đang thực hiện action)
                    var currentAdminId = int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    // Gọi service để thực hiện soft delete
                    var (success, message, user) = await _adminService.SoftDeleteUserAsync(userId, currentAdminId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = GetErrorName(message),
                                Message = message
                            }
                        };

                        return message switch
                        {
                            "User not found" => NotFound(errorResponse),
                            "Cannot delete your own account" or "Not authorized to delete another admin" => StatusCode(403, errorResponse),
                            "User is already deactivated" or "Cannot delete user with active bookings" => BadRequest(errorResponse),
                            _ => BadRequest(errorResponse)
                        };
                    }

                    // Trả về response thành công
                    var response = new DeleteUserResponse
                    {
                        Message = "Delete user success",
                        Result = new DeleteUserResult
                        {
                            UserId = userId.ToString(),
                            Deleted = true,
                            DeactivatedAt = user!.DeactivatedAt
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Ban user by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Ban a user account
            /// </remarks>
            [HttpPut("users/{user_id}/ban")] 
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(BanUserResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> BanUser(string user_id)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID format",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Lấy thông tin admin đang thực hiện action
                    var currentAdminId = int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    // Gọi service để thực hiện ban user
                    var (success, message, user) = await _adminService.BanUserAsync(userId, currentAdminId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = GetBanErrorName(message),
                                Message = message
                            }
                        };

                        return message switch
                        {
                            "User not found" => NotFound(errorResponse),
                            "User already banned" => BadRequest(errorResponse),
                            "Cannot ban your own account" or "Not authorized to ban another admin" => StatusCode(403, errorResponse),
                            _ => BadRequest(errorResponse)
                        };
                    }

                    // Trả về response thành công
                    var response = new BanUserResponse
                    {
                        Message = "Ban user success",
                        Result = new BanUserResult
                        {
                            UserId = userId.ToString(),
                            IsBanned = true,
                            BannedAt = user!.BannedAt
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Unban user by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Unban a user account
            /// </remarks>
            [HttpPut("users/{user_id}/unban")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(UnbanUserResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UnbanUser(string user_id)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID format",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Lấy thông tin admin đang thực hiện action
                    var currentAdminId = int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    // Gọi service để thực hiện unban user
                    var (success, message, user) = await _adminService.UnbanUserAsync(userId, currentAdminId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = GetUnbanErrorName(message),
                                Message = message
                            }
                        };

                        return message switch
                        {
                            "User not found" => NotFound(errorResponse),
                            "User is not banned" => BadRequest(errorResponse),
                            "Cannot unban your own account" or "Not authorized to unban another admin" => StatusCode(403, errorResponse),
                            _ => BadRequest(errorResponse)
                        };
                    }

                    // Trả về response thành công
                    var response = new UnbanUserResponse
                    {
                        Message = "Unban user success",
                        Result = new UnbanUserResult
                        {
                            UserId = userId.ToString(),
                            IsBanned = false,
                            UnbannedAt = user!.UnbannedAt
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Update user role by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Update a user's role (customer, employee, partner, manager, admin)
            /// </remarks>
            [HttpPut("users/{user_id}/role")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(UpdateUserRoleResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UpdateUserRole(
                string user_id,
                [FromBody] UpdateUserRoleRequest request)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID format",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid user ID",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Validate role parameter
                    if (string.IsNullOrWhiteSpace(request.Role))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Role is required",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "Role field cannot be empty"
                            }
                        });
                    }

                    var validRoles = new[] { "customer", "employee", "partner", "manager", "admin" };
                    if (!validRoles.Contains(request.Role.ToLower()))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid role. Available values: customer, employee, partner, manager, admin",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "Invalid role parameter"
                            }
                        });
                    }

                    // Lấy thông tin admin đang thực hiện action
                    var currentAdminId = int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    // Gọi service để thực hiện update user role
                    var (success, message, user) = await _adminService.UpdateUserRoleAsync(userId, request.Role, currentAdminId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = GetUpdateRoleErrorName(message),
                                Message = message
                            }
                        };

                        return message switch
                        {
                            "User not found" => NotFound(errorResponse),
                            "Cannot update your own role" or "Not authorized to update another admin's role" => StatusCode(403, errorResponse),
                            "Cannot update role for banned user" => BadRequest(errorResponse),
                            _ => BadRequest(errorResponse)
                        };
                    }

                    // Trả về response thành công
                    var response = new UpdateUserRoleResponse
                    {
                        Message = "Update user role success",
                        Result = new UpdateUserRoleResult
                        {
                            UserId = userId.ToString(),
                            Role = user!.UserType,
                            UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Get user by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Retrieve detailed information of a specific user by ID
            /// </remarks>
            /// <param name="user_id">User ID (required)</param>
            [HttpGet("users/{user_id}")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(AdminGetUserByIdResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> GetUserById(string user_id)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid User Id",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid User Id",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Gọi service để lấy thông tin user
                    var (success, message, user) = await _adminService.GetUserByIdAsync(userId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "NotFoundError",
                                Message = message
                            }
                        };

                        return NotFound(errorResponse);
                    }

                    // Trả về response thành công (không bao gồm password)
                    var response = new AdminGetUserByIdResponse
                    {
                        Message = "Get user successful",
                        Result = new AdminUserDetailResponse
                        {
                            UserId = user!.UserId,
                            Email = user.Email,
                            Phone = user.Phone,
                            UserType = user.UserType,
                            Fullname = user.Fullname,
                            IsActive = user.IsActive,
                            CreatedAt = user.CreatedAt,
                            EmailConfirmed = user.EmailConfirmed,
                            Username = user.Username,
                            AvataUrl = user.AvatarUrl,
                            UpdatedAt = user.UpdatedAt,
                            IsBanned = user.IsBanned,
                            BannedAt = user.BannedAt,
                            UnbannedAt = user.UnbannedAt,
                            DeactivatedAt = user.DeactivatedAt
                            // Password không được trả về
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            /// <summary>
            /// Update user by ID
            /// </summary>
            /// <remarks>
            /// Admin only - Update user information (excluding password)
            /// </remarks>
            [HttpPut("users/{user_id}")]
            [Authorize(Roles = "Admin")]
            [ProducesResponseType(typeof(AdminUpdateUserResponse), StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status400BadRequest)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status401Unauthorized)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status403Forbidden)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status409Conflict)]
            [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> UpdateUser(
                string user_id,
                [FromBody] AdminUpdateUserRequest request)
            {
                try
                {
                    // Parse user_id từ string sang int
                    if (!int.TryParse(user_id, out int userId))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid User Id",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be a valid number"
                            }
                        });
                    }

                    if (userId <= 0)
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Invalid User Id",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "User ID must be greater than 0"
                            }
                        });
                    }

                    // Lấy thông tin admin đang thực hiện action
                    var currentAdminId = int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    // Gọi service để thực hiện update user
                    var (success, message, user) = await _adminService.UpdateUserAsync(userId, request, currentAdminId);

                    if (!success)
                    {
                        var errorResponse = new ErrorAdminResponse
                        {
                            Message = message,
                            ErrorInfo = new ErrorInfo
                            {
                                Name = GetUpdateUserErrorName(message),
                                Message = message
                            }
                        };

                        return message switch
                        {
                            "User not found" => NotFound(errorResponse),
                            "Username already exists" or "Email already exists" or "Phone number already exists"
                                or "Username, email and phone number already exist"
                                or "Username and email already exist"
                                or "Username and phone number already exist"
                                or "Email and phone number already exist" => Conflict(errorResponse),
                            "Cannot update your own account" or "Not authorized to update another admin" => StatusCode(403, errorResponse),
                            _ => BadRequest(errorResponse)
                        };
                    }

                    // Trả về response thành công
                    var response = new AdminUpdateUserResponse
                    {
                        Message = "Update user successful",
                        Result = new AdminUpdateUserResult
                        {
                            UserId = userId.ToString(),
                            Email = user!.Email,
                            Phone = user.Phone,
                            UserType = user.UserType,
                            Fullname = user.Fullname,
                            IsActive = user.IsActive,
                            CreatedAt = user.CreatedAt,
                            EmailConfirmed = user.EmailConfirmed,
                            Username = user.Username,
                            AvataUrl = user.AvatarUrl,
                            UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow,
                            IsBanned = user.IsBanned,
                            BannedAt = user.BannedAt,
                            UnbannedAt = user.UnbannedAt,
                            DeactivatedAt = user.DeactivatedAt
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ErrorAdminResponse
                    {
                        Message = "An internal server error occurred",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ServerError",
                            Message = ex.Message
                        }
                    });
                }
            }

            private static string GetUpdateUserErrorName(string message)
            {
                return message switch
                {
                    "User not found" => "NotFoundError",
                    "Username already exists" or "Email already exists" or "Phone number already exists"
                        or "Username, email and phone number already exist"
                        or "Username and email already exist"
                        or "Username and phone number already exist"
                        or "Email and phone number already exist" => "ConflictError",
                    "Cannot update your own account" or "Not authorized to update another admin" => "AuthorizationError",
                    _ => "BusinessRuleError"
                };
            }

            private static string GetUpdateRoleErrorName(string message)
            {
                return message switch
                {
                    "User not found" => "NotFoundError",
                    "Cannot update your own role" or "Not authorized to update another admin's role" => "AuthorizationError",
                    "Cannot update role for banned user" => "BusinessRuleError",
                    _ => "BusinessRuleError"
                };
            }
            private static string GetUnbanErrorName(string message)
            {
                return message switch
                {
                    "User not found" => "NotFoundError",
                    "User is not banned" => "BusinessRuleError",
                    "Cannot unban your own account" or "Not authorized to unban another admin" => "AuthorizationError",
                    _ => "BusinessRuleError"
                };
            }

            private static string GetBanErrorName(string message)
            {
                return message switch
                {
                    "User not found" => "NotFoundError",
                    "User already banned" => "BusinessRuleError",
                    "Cannot ban your own account" or "Not authorized to ban another admin" => "AuthorizationError",
                    _ => "BusinessRuleError"
                };
            }

            private static string GetErrorName(string message)
            {
                return message switch
                {
                    "User not found" => "NotFoundError",
                    "Cannot delete your own account" or "Not authorized to delete another admin" => "AuthorizationError",
                    "User is already deactivated" or "Cannot delete user with active bookings" => "BusinessRuleError",
                    _ => "BusinessRuleError"
                };
            }

       
            }
        }
