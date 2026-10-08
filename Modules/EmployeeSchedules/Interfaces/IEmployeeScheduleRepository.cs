using SaborExpress.Modules.EmployeeSchedules.Models;

namespace SaborExpress.Modules.EmployeeSchedules.Interfaces
{
    public interface IEmployeeScheduleRepository
    {
        Task<EmployeeSchedule?> GetByIdAsync(int id);
        Task<List<EmployeeSchedule>> GetByEmployeeAsync(int employeeId);
        Task<List<EmployeeSchedule>> GetByBranchAsync(int branchId);
        Task<List<EmployeeSchedule>> GetByBranchAndDateAsync(int branchId, DateOnly date);
        Task<List<EmployeeSchedule>> GetByEmployeeAndDateAsync(int employeeId, DateOnly date);
        Task<EmployeeSchedule> AddAsync(EmployeeSchedule schedule);
        Task UpdateAsync(EmployeeSchedule schedule);
        Task DeleteAsync(EmployeeSchedule schedule);
        Task<bool> EmployeeExistsAsync(int employeeId);
        Task<bool> BranchExistsAsync(int branchId);

        Task<List<EmployeeSchedule>> GetShiftsEndingSoonAsync(DateTime fromTime, DateTime toTime);
        Task MarkReminderSentAsync(int scheduleId);

        Task<List<EmployeeSchedule>> GetShiftsEndedTodayAsync(DateTime now);

        Task<List<EmployeeSchedule>> GetActiveCookShiftsToNotifyAsync(DateTime now);
        Task MarkKitchenLinkSentAsync(int scheduleId, string accessToken);

        Task<EmployeeSchedule?> GetActiveScheduleByTokenAsync(string token, DateTime now);

        Task<List<int>> GetEmployeeIdsWithSchedulesAsync();

        Task<List<int>> GetRequiredRoleIdsAsync(int employeeId, DateOnly fromDate);
        Task<List<int>> GetAllRoleIdsEverAssignedAsync(int employeeId);           
    }
}