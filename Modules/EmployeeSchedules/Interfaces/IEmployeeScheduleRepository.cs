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

        // Nuevo: turnos de hoy que ya terminaron (para el paso de auto-renovacion)
        Task<List<EmployeeSchedule>> GetShiftsEndedTodayAsync();
    }
}