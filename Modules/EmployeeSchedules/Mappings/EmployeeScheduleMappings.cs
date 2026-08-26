using SaborExpress.Modules.EmployeeSchedules.DTOs;
using SaborExpress.Modules.EmployeeSchedules.Models;

namespace SaborExpress.Modules.EmployeeSchedules.Mappings
{
    public static class EmployeeScheduleMappings
    {
        public static EmployeeScheduleResponseDto ToResponseDto(this EmployeeSchedule entity)
        {
            return new EmployeeScheduleResponseDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee != null
                    ? $"{entity.Employee.Name} {entity.Employee.LastName}"
                    : string.Empty,
                BranchId = entity.BranchId,
                BranchName = entity.Branch?.Name ?? string.Empty,
                RoleId = entity.RoleId,
                RoleName = entity.Role?.Name ?? string.Empty,
                ShiftDate = entity.ShiftDate,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Status = entity.Status,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedAt
            };
        }

        public static EmployeeSchedule ToModel(this CreateEmployeeScheduleDto dto)
        {
            return new EmployeeSchedule
            {
                EmployeeId = dto.EmployeeId,
                BranchId = dto.BranchId,
                RoleId = dto.RoleId,
                ShiftDate = dto.ShiftDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Notes = dto.Notes
            };
        }
    }
}