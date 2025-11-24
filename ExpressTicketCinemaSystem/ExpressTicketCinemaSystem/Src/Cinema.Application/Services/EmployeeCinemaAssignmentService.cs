using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public interface IEmployeeCinemaAssignmentService
    {
        Task AssignCinemaToEmployeeAsync(int partnerId, int employeeId, int cinemaId, int assignedByUserId);
        Task UnassignCinemaFromEmployeeAsync(int partnerId, int employeeId, int cinemaId);
        Task<List<int>> GetAssignedCinemaIdsAsync(int employeeId);
        Task<bool> HasAccessToCinemaAsync(int employeeId, int cinemaId);
        Task<List<Infrastructure.Models.Cinema>> GetAssignedCinemasAsync(int employeeId);
    }

    public class EmployeeCinemaAssignmentService : IEmployeeCinemaAssignmentService
    {
        private readonly CinemaDbCoreContext _context;

        public EmployeeCinemaAssignmentService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        public async Task AssignCinemaToEmployeeAsync(int partnerId, int employeeId, int cinemaId, int assignedByUserId)
        {
            // Validate employee belongs to partner
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId && e.RoleType == "Staff");

            if (employee == null)
            {
                throw new NotFoundException("Không tìm thấy nhân viên Staff hoặc không thuộc quyền quản lý của bạn");
            }

            // Validate cinema belongs to partner
            var cinema = await _context.Cinemas
                .FirstOrDefaultAsync(c => c.CinemaId == cinemaId && c.PartnerId == partnerId);

            if (cinema == null)
            {
                throw new NotFoundException("Không tìm thấy rạp hoặc không thuộc quyền quản lý của bạn");
            }

            // Check if there's already an assignment for this cinema (even if inactive)
            var existingAssignment = await _context.EmployeeCinemaAssignments
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.CinemaId == cinemaId);

            if (existingAssignment != null)
            {
                // Nếu đã có assignment nhưng đang inactive, reactivate nó
                if (!existingAssignment.IsActive)
                {
                    existingAssignment.IsActive = true;
                    existingAssignment.AssignedAt = DateTime.UtcNow;
                    existingAssignment.AssignedBy = assignedByUserId;
                    existingAssignment.UnassignedAt = null;
                }
                // Nếu đã active rồi thì không làm gì (đã được phân quyền rồi)
            }
            else
            {
                // Create new assignment - một staff có thể quản lý nhiều cinema
                var assignment = new EmployeeCinemaAssignment
                {
                    EmployeeId = employeeId,
                    CinemaId = cinemaId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = assignedByUserId,
                    IsActive = true
                };

                _context.EmployeeCinemaAssignments.Add(assignment);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UnassignCinemaFromEmployeeAsync(int partnerId, int employeeId, int cinemaId)
        {
            // Validate employee belongs to partner
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PartnerId == partnerId);

            if (employee == null)
            {
                throw new NotFoundException("Không tìm thấy nhân viên hoặc không thuộc quyền quản lý của bạn");
            }

            var assignment = await _context.EmployeeCinemaAssignments
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.CinemaId == cinemaId && a.IsActive);

            if (assignment == null)
            {
                throw new NotFoundException("Không tìm thấy phân quyền này");
            }

            assignment.IsActive = false;
            assignment.UnassignedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetAssignedCinemaIdsAsync(int employeeId)
        {
            return await _context.EmployeeCinemaAssignments
                .Where(a => a.EmployeeId == employeeId && a.IsActive)
                .Select(a => a.CinemaId)
                .ToListAsync();
        }

        public async Task<bool> HasAccessToCinemaAsync(int employeeId, int cinemaId)
        {
            return await _context.EmployeeCinemaAssignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.CinemaId == cinemaId && a.IsActive);
        }

        public async Task<List<Infrastructure.Models.Cinema>> GetAssignedCinemasAsync(int employeeId)
        {
            return await _context.EmployeeCinemaAssignments
                .Include(a => a.Cinema)
                .Where(a => a.EmployeeId == employeeId && a.IsActive)
                .Select(a => a.Cinema)
                .ToListAsync();
        }
    }
}

