using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Staff.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Permission.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class StaffService : IStaffService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IPermissionService _permissionService;

        public StaffService(CinemaDbCoreContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public async Task<StaffProfileResponse> GetStaffProfileAsync(int userId)
        {
            // Lấy thông tin Employee từ UserId
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Partner)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

            if (employee == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhân viên hoặc tài khoản chưa được kích hoạt");
            }

            // Lấy danh sách cinemas được phân công
            var assignedCinemas = await _context.EmployeeCinemaAssignments
                .Include(a => a.Cinema)
                .Include(a => a.AssignedByUser)
                .Where(a => a.EmployeeId == employee.EmployeeId && a.IsActive)
                .Select(a => new AssignedCinemaInfo
                {
                    CinemaId = a.CinemaId,
                    CinemaName = a.Cinema.CinemaName,
                    Address = a.Cinema.Address,
                    City = a.Cinema.City,
                    District = a.Cinema.District,
                    AssignedAt = a.AssignedAt,
                    AssignedByUserId = a.AssignedBy,
                    AssignedByEmail = a.AssignedByUser != null ? a.AssignedByUser.Email : null,
                    AssignedByName = a.AssignedByUser != null ? a.AssignedByUser.Fullname : null
                })
                .ToListAsync();

            // Lấy danh sách permissions được cấp - sử dụng PermissionService để có logic nhóm theo cinema
            var permissionsListResponse = await _permissionService.GetEmployeePermissionsAsync(employee.EmployeeId, null);

            // Convert từ CinemaPermissionsGroup sang GrantedPermissionInfo để giữ nguyên response structure
            var grantedPermissions = new List<GrantedPermissionInfo>();
            foreach (var cinemaGroup in permissionsListResponse.CinemaPermissions)
            {
                foreach (var perm in cinemaGroup.Permissions)
                {
                    grantedPermissions.Add(new GrantedPermissionInfo
                    {
                        PermissionId = perm.PermissionId,
                        PermissionCode = perm.PermissionCode,
                        PermissionName = perm.PermissionName,
                        ResourceType = perm.ResourceType,
                        ActionType = perm.ActionType,
                        Description = perm.Description,
                        CinemaId = cinemaGroup.CinemaId,
                        CinemaName = cinemaGroup.CinemaName,
                        GrantedAt = perm.GrantedAt,
                        GrantedByUserId = perm.GrantedByUserId,
                        GrantedByEmail = perm.GrantedByEmail,
                        GrantedByName = perm.GrantedByName,
                        IsActive = perm.IsActive
                    });
                }
            }

            return new StaffProfileResponse
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Email = employee.User.Email,
                RoleType = employee.RoleType ?? "Staff",
                IsActive = employee.IsActive,
                PartnerId = employee.PartnerId,
                PartnerName = employee.Partner?.PartnerName ?? "Unknown",
                AssignedCinemas = assignedCinemas,
                GrantedPermissions = grantedPermissions.OrderBy(p => p.CinemaName).ThenBy(p => p.ResourceType).ThenBy(p => p.ActionType).ToList()
            };
        }

        public async Task<EmployeePermissionsListResponse> GetMyPermissionsAsync(int userId, List<int>? cinemaIds = null)
        {
            // Lấy thông tin Employee từ UserId
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

            if (employee == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin nhân viên hoặc tài khoản chưa được kích hoạt");
            }

            // Sử dụng PermissionService để lấy permissions với logic nhóm theo cinema
            return await _permissionService.GetEmployeePermissionsAsync(employee.EmployeeId, cinemaIds);
        }
    }
}

