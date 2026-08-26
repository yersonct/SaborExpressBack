using SaborExpress.Modules.EmployeeSchedules.DTOs;

namespace SaborExpress.Modules.EmployeeSchedules.Validators
{
    public class EmployeeScheduleValidator
    {
        public void ValidateCreate(CreateEmployeeScheduleDto dto)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Debes indicar un empleado valido.");

            if (dto.BranchId <= 0)
                throw new ArgumentException("Debes indicar una sucursal valida.");

            if (dto.RoleId <= 0)
                throw new ArgumentException("Debes indicar un rol valido para el turno.");

            if (dto.ShiftDate == default)
                throw new ArgumentException("Debes indicar la fecha del turno.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("La hora de fin debe ser mayor a la hora de inicio.");

            if (!string.IsNullOrEmpty(dto.Notes) && dto.Notes.Length > 300)
                throw new ArgumentException("Las notas no pueden superar los 300 caracteres.");
        }

        public void ValidateUpdate(UpdateEmployeeScheduleDto dto)
        {
            if (dto.RoleId <= 0)
                throw new ArgumentException("Debes indicar un rol valido para el turno.");

            if (dto.ShiftDate == default)
                throw new ArgumentException("Debes indicar la fecha del turno.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("La hora de fin debe ser mayor a la hora de inicio.");

            if (!string.IsNullOrEmpty(dto.Notes) && dto.Notes.Length > 300)
                throw new ArgumentException("Las notas no pueden superar los 300 caracteres.");
        }
    }
}