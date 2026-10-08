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

        Task<int> TransferEmployeeToBranchAsync(int employeeId, int newBranchId, int currentUserId);

        Task<CurrentShiftResponseDto> GetCurrentShiftAsync(int employeeId);
        Task RevokeExpiredRolesAsync();

        Task<List<EmployeeScheduleResponseDto>> GetByEmployeeAsync(int employeeId, int currentUserId);
        Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId, int currentUserId);
        Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId, int currentUserId);
    }
}