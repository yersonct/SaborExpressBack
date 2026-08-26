using SaborExpress.Modules.EmployeeSchedules.DTOs;

namespace SaborExpress.Modules.EmployeeSchedules.Interfaces
{
    public interface IEmployeeScheduleService
    {
        Task<List<EmployeeScheduleResponseDto>> GetByEmployeeAsync(int employeeId);
        Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId);
        Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId);
        Task<EmployeeScheduleResponseDto> CreateAsync(CreateEmployeeScheduleDto dto, int currentUserId);
        Task<EmployeeScheduleResponseDto> UpdateAsync(int id, UpdateEmployeeScheduleDto dto, int currentUserId);
        Task<EmployeeScheduleResponseDto> UpdateStatusAsync(int id, UpdateScheduleStatusDto dto, int currentUserId);
        Task DeleteAsync(int id, int currentUserId);

          // Nuevo: dice si el empleado tiene turno activo AHORA MISMO, y con que rol
        Task<CurrentShiftResponseDto> GetCurrentShiftAsync(int employeeId);
    }
}