using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Models;

namespace SaborExpress.Modules.EmployeeSchedules.Repositories
{
    public class EmployeeScheduleRepository : IEmployeeScheduleRepository
    {
        private readonly AppDbContext _context;

        public EmployeeScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmployeeSchedule?> GetByIdAsync(int id)
        {
            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Branch)
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<EmployeeSchedule>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Branch)
                .Include(s => s.Role)
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.ShiftDate)
                .ToListAsync();
        }

        public async Task<List<EmployeeSchedule>> GetByBranchAsync(int branchId)
        {
            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Role)
                .Where(s => s.BranchId == branchId)
                .OrderBy(s => s.ShiftDate)
                .ToListAsync();
        }

        public async Task<List<EmployeeSchedule>> GetByBranchAndDateAsync(int branchId, DateOnly date)
        {
            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Role)
                .Where(s => s.BranchId == branchId && s.ShiftDate == date)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<EmployeeSchedule>> GetByEmployeeAndDateAsync(int employeeId, DateOnly date)
        {
            return await _context.EmployeeSchedules
                .Where(s => s.EmployeeId == employeeId && s.ShiftDate == date)
                .ToListAsync();
        }

        public async Task<EmployeeSchedule> AddAsync(EmployeeSchedule schedule)
        {
            _context.EmployeeSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task UpdateAsync(EmployeeSchedule schedule)
        {
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.EmployeeSchedules.Update(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(EmployeeSchedule schedule)
        {
            _context.EmployeeSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmployeeExistsAsync(int employeeId)
        {
            return await _context.Employees.AnyAsync(e => e.Id == employeeId);
        }

        public async Task<bool> BranchExistsAsync(int branchId)
        {
            return await _context.Branches.AnyAsync(b => b.Id == branchId);
        }
        public async Task<List<EmployeeSchedule>> GetShiftsEndingSoonAsync(DateTime fromTime, DateTime toTime)
        {
            var today = DateOnly.FromDateTime(fromTime);
            var fromTimeOnly = TimeOnly.FromDateTime(fromTime);
            var toTimeOnly = TimeOnly.FromDateTime(toTime);

            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Role)
                .Where(s => s.ShiftDate == today
                    && s.Status == Enum.ScheduleStatus.Programado
                    && !s.ReminderSent
                    && s.EndTime >= fromTimeOnly
                    && s.EndTime <= toTimeOnly)
                .ToListAsync();
        }

        public async Task<List<int>> GetEmployeeIdsWithSchedulesAsync()
        {
            return await _context.EmployeeSchedules
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<int>> GetRequiredRoleIdsAsync(int employeeId, DateOnly fromDate)
        {
            return await _context.EmployeeSchedules
                .Where(s => s.EmployeeId == employeeId && s.ShiftDate >= fromDate)
                .Select(s => s.RoleId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<int>> GetAllRoleIdsEverAssignedAsync(int employeeId)
        {
            return await _context.EmployeeSchedules
                .Where(s => s.EmployeeId == employeeId)
                .Select(s => s.RoleId)
                .Distinct()
                .ToListAsync();
        }

        public async Task MarkReminderSentAsync(int scheduleId)
        {
            var schedule = await _context.EmployeeSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                schedule.ReminderSent = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeSchedule>> GetShiftsEndedTodayAsync(DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Role)
                .Where(s => s.ShiftDate == today
                    && s.Status == Enum.ScheduleStatus.Programado
                    && s.EndTime <= nowTime)
                .ToListAsync();
        }

        public async Task<List<EmployeeSchedule>> GetActiveCookShiftsToNotifyAsync(DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            return await _context.EmployeeSchedules
                .Include(s => s.Employee).ThenInclude(e => e.User)
                .Include(s => s.Branch)
                .Include(s => s.Role)
                .Where(s => s.ShiftDate == today
                    && s.Status == Enum.ScheduleStatus.Programado
                    && !s.KitchenLinkSent
                    && s.Role.Name == SaborExpress.Shared.Constants.RoleNames.Cocinero
                    && s.StartTime <= nowTime
                    && s.EndTime >= nowTime)
                .ToListAsync();
        }

        public async Task MarkKitchenLinkSentAsync(int scheduleId, string accessToken)
        {
            var schedule = await _context.EmployeeSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                schedule.KitchenLinkSent = true;
                schedule.KitchenAccessToken = accessToken;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<EmployeeSchedule?> GetActiveScheduleByTokenAsync(string token, DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            return await _context.EmployeeSchedules
                .Include(s => s.Branch)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.KitchenAccessToken == token
                    && s.ShiftDate == today
                    && s.Status == Enum.ScheduleStatus.Programado
                    && s.StartTime <= nowTime
                    && s.EndTime >= nowTime);
        }
    }
}