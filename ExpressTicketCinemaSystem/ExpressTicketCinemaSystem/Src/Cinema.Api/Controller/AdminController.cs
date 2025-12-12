using ExpressTicketCinemaSystem.Src.Cinema.Application.Services;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Request;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using static ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum.AdminEnum;

namespace ExpressTicketCinemaSystem.Src.Cinema.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly IAuditLogService _auditLogService;

        public AdminController(AdminService adminService, IAuditLogService auditLogService)
        {
            _adminService = adminService;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Danh sách audit log có phân trang và bộ lọc
        /// </summary>
        [HttpGet("audit-logs")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(AdminAuditLogListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AdminAuditLogFilterRequest request, CancellationToken cancellationToken)
        {
            var (logs, total) = await _auditLogService.GetAuditLogsAsync(request, cancellationToken);

            var limit = Math.Clamp(request.Limit, 1, 200);
            var page = request.Page <= 0 ? 1 : request.Page;
            var totalPages = (int)Math.Ceiling((double)total / limit);

            var response = new AdminAuditLogListResponse
            {
                Result = new AdminAuditLogListResult
                {
                    Logs = logs.Select(log => new AdminAuditLogItemResponse
                    {
                        LogId = log.LogId,
                        UserId = log.UserId,
                        Role = log.Role,
                        Action = log.Action,
                        TableName = log.TableName,
                        RecordId = log.RecordId,
                        Timestamp = DateTimeHelper.ToVietnamTime(log.Timestamp), // Chuyển sang giờ VN
                        Before = DeserializePayload(log.BeforeData),
                        After = DeserializePayload(log.AfterData),
                        Metadata = DeserializePayload(log.Metadata),
                        IpAddress = log.IpAddress,
                        UserAgent = log.UserAgent
                    }).ToList(),
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = totalPages
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Chi tiết một audit log
        /// </summary>
        [HttpGet("audit-logs/{logId:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(AdminAuditLogDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorAdminResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuditLogById(int logId, CancellationToken cancellationToken)
        {
            var log = await _auditLogService.GetAuditLogByIdAsync(logId, cancellationToken);
            if (log == null)
            {
                return NotFound(new ErrorAdminResponse
                {
                    Message = "Không tìm thấy audit log",
                    ErrorInfo = new ErrorInfo
                    {
                        Name = "NotFoundError",
                        Message = "Audit log không tồn tại"
                    }
                });
            }

            var response = new AdminAuditLogDetailResponse
            {
                Result = new AdminAuditLogItemResponse
                {
                    LogId = log.LogId,
                    UserId = log.UserId,
                    Role = log.Role,
                    Action = log.Action,
                    TableName = log.TableName,
                    RecordId = log.RecordId,
                    Timestamp = DateTimeHelper.ToVietnamTime(log.Timestamp), // Chuyển sang giờ VN
                    Before = DeserializePayload(log.BeforeData),
                    After = DeserializePayload(log.AfterData),
                    Metadata = DeserializePayload(log.Metadata),
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent
                }
            };

            return Ok(response);
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
        /// <param name="role">Filter by user role (Available values: user, staff, admin, partner, manager)</param>
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
            [FromQuery] int? verify = null, // Đổi từ VerifyStatus? sang int?
            [FromQuery(Name = "sort_by")] string sortBy = "created_at",
            [FromQuery(Name = "sort_order")] string sortOrder = "desc")
        {
            try
            {
                // Validate role parameter - không phân biệt chữ hoa/chữ thường
                if (!string.IsNullOrEmpty(role))
                {
                    var validRoles = new List<string> { "user", "staff", "admin", "partner", "manager" };
                    var normalizedRole = role.ToLower();

                    if (!validRoles.Contains(normalizedRole))
                    {
                        return BadRequest(new ErrorAdminResponse
                        {
                            Message = "Giá trị vai trò không hợp lệ. Các giá trị có sẵn: user, staff, admin, partner, manager",
                            ErrorInfo = new ErrorInfo
                            {
                                Name = "ValidationError",
                                Message = "Tham số vai trò không hợp lệ"
                            }
                        });
                    }

                    // Gán lại giá trị role đã được chuẩn hóa
                    role = normalizedRole;
                }

                // Validate verify parameter
                if (verify.HasValue && verify.Value != 0 && verify.Value != 1 && verify.Value != 2)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "Giá trị xác minh không hợp lệ. Các giá trị có sẵn: 0=chưa xác minh, 1=đã xác minh, 2=đã cấm",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "Tham số xác minh không hợp lệ"
                        }
                    });
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
                        // Sửa logic verify status theo yêu cầu mới
                        Verify = user.IsBanned ? 2 : (user.EmailConfirmed ? 1 : 0),
                        CreatedAt = user.CreatedAt,
                        Stats = stats
                    };
                    userResponses.Add(userResponse);
                }

                var response = new AdminPaginatedUserListResponse
                {
                    Message = "Lấy danh sách người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ.",
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
                        Message = "Định dạng ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
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
                        "Không tìm thấy người dùng" => NotFound(errorResponse),
                        "Không thể xóa tài khoản của chính bạn" or "Không được ủy quyền để xóa quản trị viên khác" => StatusCode(403, errorResponse),
                        "Người dùng đã bị vô hiệu hóa" or "Không thể xóa người dùng có đặt chỗ đang hoạt động" => BadRequest(errorResponse),
                        _ => BadRequest(errorResponse)
                    };
                }

                // Trả về response thành công
                var response = new DeleteUserResponse
                {
                    Message = "Xóa người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
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
                        Message = "Định dạng ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
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
                        "Không tìm thấy người dùng" => NotFound(errorResponse),
                        "Người dùng đã bị cấm" => BadRequest(errorResponse),
                        "Không thể cấm tài khoản của chính bạn" or "Không được ủy quyền để cấm quản trị viên khác" => StatusCode(403, errorResponse),
                        _ => BadRequest(errorResponse)
                    };
                }

                // Trả về response thành công
                var response = new BanUserResponse
                {
                    Message = "Cấm người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
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
                        Message = "Định dạng ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
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
                        "Không tìm thấy người dùng" => NotFound(errorResponse),
                        "Người dùng không bị cấm" => BadRequest(errorResponse),
                        "Không thể bỏ cấm tài khoản của chính bạn" or "Không được ủy quyền để bỏ cấm quản trị viên khác" => StatusCode(403, errorResponse),
                        _ => BadRequest(errorResponse)
                    };
                }

                // Trả về response thành công
                var response = new UnbanUserResponse
                {
                    Message = "Bỏ cấm người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
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
        /// Admin only - Update a user's role (user, staff, partner, manager, admin)
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
                        Message = "Định dạng ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
                        }
                    });
                }

                // Validate role parameter
                if (string.IsNullOrWhiteSpace(request.Role))
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "Vai trò là bắt buộc",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "Trường vai trò không được để trống"
                        }
                    });
                }

                var validRoles = new[] { "user", "staff", "partner", "manager", "admin" };
                if (!validRoles.Contains(request.Role.ToLower()))
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "Vai trò không hợp lệ. Các giá trị có sẵn: user, staff, partner, manager, admin",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "Tham số vai trò không hợp lệ"
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
                        "Không tìm thấy người dùng" => NotFound(errorResponse),
                        "Không thể cập nhật vai trò của chính bạn" or "Không được ủy quyền để cập nhật vai trò của quản trị viên khác" => StatusCode(403, errorResponse),
                        "Không thể cập nhật vai trò cho người dùng bị cấm" => BadRequest(errorResponse),
                        _ => BadRequest(errorResponse)
                    };
                }

                // Trả về response thành công
                var response = new UpdateUserRoleResponse
                {
                    Message = "Cập nhật vai trò người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
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
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
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
                    Message = "Lấy thông tin người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
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
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải là số hợp lệ"
                        }
                    });
                }

                if (userId <= 0)
                {
                    return BadRequest(new ErrorAdminResponse
                    {
                        Message = "ID người dùng không hợp lệ",
                        ErrorInfo = new ErrorInfo
                        {
                            Name = "ValidationError",
                            Message = "ID người dùng phải lớn hơn 0"
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
                        "Không tìm thấy người dùng" => NotFound(errorResponse),
                        "Tên người dùng đã tồn tại" or "Email đã tồn tại" or "Số điện thoại đã tồn tại"
                            or "Tên người dùng, email và số điện thoại đã tồn tại"
                            or "Tên người dùng và email đã tồn tại"
                            or "Tên người dùng và số điện thoại đã tồn tại"
                            or "Email và số điện thoại đã tồn tại" => Conflict(errorResponse),
                        "Không thể cập nhật tài khoản của chính bạn" or "Không được ủy quyền để cập nhật quản trị viên khác" => StatusCode(403, errorResponse),
                        _ => BadRequest(errorResponse)
                    };
                }

                // Trả về response thành công
                var response = new AdminUpdateUserResponse
                {
                    Message = "Cập nhật người dùng thành công",
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
                    Message = "Đã xảy ra lỗi máy chủ nội bộ",
                    ErrorInfo = new ErrorInfo
                    {
                        Name = "ServerError",
                        Message = ex.Message
                    }
                });
            }
        }

        private static object? DeserializePayload(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<object>(json);
            }
            catch
            {
                return json;
            }
        }

        private static string GetUpdateUserErrorName(string message)
        {
            return message switch
            {
                "Không tìm thấy người dùng" => "NotFoundError",
                "Tên người dùng đã tồn tại" or "Email đã tồn tại" or "Số điện thoại đã tồn tại"
                    or "Tên người dùng, email và số điện thoại đã tồn tại"
                    or "Tên người dùng và email đã tồn tại"
                    or "Tên người dùng và số điện thoại đã tồn tại"
                    or "Email và số điện thoại đã tồn tại" => "ConflictError",
                "Không thể cập nhật tài khoản của chính bạn" or "Không được ủy quyền để cập nhật quản trị viên khác" => "AuthorizationError",
                _ => "BusinessRuleError"
            };
        }

        private static string GetUpdateRoleErrorName(string message)
        {
            return message switch
            {
                "Không tìm thấy người dùng" => "NotFoundError",
                "Không thể cập nhật vai trò của chính bạn" or "Không được ủy quyền để cập nhật vai trò của quản trị viên khác" => "AuthorizationError",
                "Không thể cập nhật vai trò cho người dùng bị cấm" => "BusinessRuleError",
                _ => "BusinessRuleError"
            };
        }
        private static string GetUnbanErrorName(string message)
        {
            return message switch
            {
                "Không tìm thấy người dùng" => "NotFoundError",
                "Người dùng không bị cấm" => "BusinessRuleError",
                "Không thể bỏ cấm tài khoản của chính bạn" or "Không được ủy quyền để bỏ cấm quản trị viên khác" => "AuthorizationError",
                _ => "BusinessRuleError"
            };
        }

        private static string GetBanErrorName(string message)
        {
            return message switch
            {
                "Không tìm thấy người dùng" => "NotFoundError",
                "Người dùng đã bị cấm" => "BusinessRuleError",
                "Không thể cấm tài khoản của chính bạn" or "Không được ủy quyền để cấm quản trị viên khác" => "AuthorizationError",
                _ => "BusinessRuleError"
            };
        }

        private static string GetErrorName(string message)
        {
            return message switch
            {
                "Không tìm thấy người dùng" => "NotFoundError",
                "Không thể xóa tài khoản của chính bạn" or "Không được ủy quyền để xóa quản trị viên khác" => "AuthorizationError",
                "Người dùng đã bị vô hiệu hóa" or "Không thể xóa người dùng có đặt chỗ đang hoạt động" => "BusinessRuleError",
                _ => "BusinessRuleError"
            };
        }

    }
}