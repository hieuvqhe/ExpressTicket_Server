using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Partner.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class EmployeeManagementService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;

        public EmployeeManagementService(
            CinemaDbCoreContext context,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public async Task<EmployeeResponse> CreateEmployeeAsync(int partnerId, CreateEmployeeRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();

            // Validation
            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new ValidationError { Msg = "Họ và tên là bắt buộc", Path = "fullName" };

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["email"] = new ValidationError { Msg = "Email là bắt buộc", Path = "email" };
            else if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = new ValidationError { Msg = "Định dạng email không hợp lệ", Path = "email" };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new ValidationError { Msg = "Mật khẩu là bắt buộc", Path = "password" };
            else if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,12}$"))
                errors["password"] = new ValidationError { Msg = "Mật khẩu phải từ 6-12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt", Path = "password" };

            if (request.Password != request.ConfirmPassword)
                errors["confirmPassword"] = new ValidationError { Msg = "Mật khẩu và xác nhận mật khẩu không khớp", Path = "confirmPassword" };

            var validRoles = new[] { "Staff", "Marketing", "Cashier" };
            if (string.IsNullOrWhiteSpace(request.RoleType) || !validRoles.Contains(request.RoleType))
                errors["roleType"] = new ValidationError { Msg = "RoleType phải là Staff, Marketing hoặc Cashier", Path = "roleType" };

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new ConflictException("email", "Email đã tồn tại");

            // Check if user already has an employee record
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null && await _context.Employees.AnyAsync(e => e.UserId == existingUser.UserId))
                throw new ConflictException("email", "Người dùng này đã là nhân viên");

            if (errors.Any())
                throw new ValidationException(errors);

            // Create User
            var user = new User
            {
                Fullname = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Username = request.Email,
                UserType = request.RoleType,
                Password = _passwordHasher.HashPassword(null, request.Password),
                IsActive = true,
                EmailConfirmed = true, // Partner creates employees, so email is considered confirmed
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Employee
            var employee = new Employee
            {
                PartnerId = partnerId,
                UserId = user.UserId,
                FullName = request.FullName,
                RoleType = request.RoleType,
                HireDate = request.HireDate,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            employee.User = user;
            await _auditLogService.LogEntityChangeAsync(
                action: "PARTNER_CREATE_EMPLOYEE",
                tableName: "Employee",
                recordId: employee.EmployeeId,
                beforeData: null,
                afterData: BuildEmployeeSnapshot(employee),
                metadata: new { partnerId });

            return new EmployeeResponse
            {
                EmployeeId = employee.EmployeeId,
                UserId = user.UserId,
                FullName = employee.FullName,
                Email = user.Email,
                Phone = user.Phone ?? "",
                RoleType = employee.RoleType,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            };
        }

        public async Task<EmployeeResponse> UpdateEmployeeAsync(int partnerId, int employeeId, UpdateEmployeeRequest request)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId);

            if (employee == null)
                throw new NotFoundException("Không tìm thấy nhân viên");

            var errors = new Dictionary<string, ValidationError>();
            var beforeSnapshot = BuildEmployeeSnapshot(employee);

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                employee.FullName = request.FullName;
                employee.User.Fullname = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
                employee.User.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.RoleType))
            {
                var validRoles = new[] { "Staff", "Marketing", "Cashier" };
                if (!validRoles.Contains(request.RoleType))
                    errors["roleType"] = new ValidationError { Msg = "RoleType phải là Staff, Marketing hoặc Cashier", Path = "roleType" };
                else
                {
                    employee.RoleType = request.RoleType;
                    employee.User.UserType = request.RoleType;
                }
            }

            if (request.IsActive.HasValue)
            {
                employee.IsActive = request.IsActive.Value;
                employee.User.IsActive = request.IsActive.Value;
            }

            if (errors.Any())
                throw new ValidationException(errors);

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "PARTNER_UPDATE_EMPLOYEE",
                tableName: "Employee",
                recordId: employee.EmployeeId,
                beforeData: beforeSnapshot,
                afterData: BuildEmployeeSnapshot(employee),
                metadata: new { partnerId });

            return new EmployeeResponse
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                FullName = employee.FullName,
                Email = employee.User.Email,
                Phone = employee.User.Phone ?? "",
                RoleType = employee.RoleType,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            };
        }

        public async Task<PaginatedEmployeesResponse> GetEmployeesAsync(
            int partnerId,
            int page = 1,
            int limit = 10,
            string? roleType = null,
            bool? isActive = null,
            string? search = null,
            string sortBy = "fullName",
            string sortOrder = "asc")
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _context.Employees
                .Include(e => e.User)
                .Where(e => e.PartnerId == partnerId);

            if (!string.IsNullOrWhiteSpace(roleType))
                query = query.Where(e => e.RoleType == roleType);

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.FullName.Contains(search) ||
                    e.User.Email.Contains(search) ||
                    (e.User.Phone != null && e.User.Phone.Contains(search)));
            }

            // Sorting
            query = sortBy.ToLower() switch
            {
                "fullname" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
                "email" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(e => e.User.Email) : query.OrderBy(e => e.User.Email),
                "roletype" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(e => e.RoleType) : query.OrderBy(e => e.RoleType),
                "hiredate" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate),
                _ => query.OrderBy(e => e.FullName)
            };

            var total = await query.CountAsync();
            var employees = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(e => new EmployeeResponse
                {
                    EmployeeId = e.EmployeeId,
                    UserId = e.UserId,
                    FullName = e.FullName,
                    Email = e.User.Email,
                    Phone = e.User.Phone ?? "",
                    RoleType = e.RoleType,
                    HireDate = e.HireDate,
                    IsActive = e.IsActive
                })
                .ToListAsync();

            return new PaginatedEmployeesResponse
            {
                Employees = employees,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = limit,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        public async Task<EmployeeResponse> GetEmployeeByIdAsync(int partnerId, int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId);

            if (employee == null)
                throw new NotFoundException("Không tìm thấy nhân viên");

            return new EmployeeResponse
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                FullName = employee.FullName,
                Email = employee.User.Email,
                Phone = employee.User.Phone ?? "",
                RoleType = employee.RoleType,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            };
        }

        public async Task DeleteEmployeeAsync(int partnerId, int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId);

            if (employee == null)
                throw new NotFoundException("Không tìm thấy nhân viên");

            var beforeSnapshot = BuildEmployeeSnapshot(employee);

            // Soft delete: set IsActive = false
            employee.IsActive = false;
            employee.User.IsActive = false;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "PARTNER_DELETE_EMPLOYEE",
                tableName: "Employee",
                recordId: employee.EmployeeId,
                beforeData: beforeSnapshot,
                afterData: BuildEmployeeSnapshot(employee),
                metadata: new { partnerId });
        }

        private static object BuildEmployeeSnapshot(Employee employee)
        {
            return new
            {
                employee.EmployeeId,
                employee.PartnerId,
                employee.UserId,
                employee.FullName,
                employee.RoleType,
                employee.HireDate,
                employee.IsActive,
                User = employee.User == null
                    ? null
                    : new
                    {
                        employee.User.UserId,
                        employee.User.Email,
                        employee.User.Fullname,
                        employee.User.Phone,
                        employee.User.UserType,
                        employee.User.IsActive
                    }
            };
        }
    }
}

