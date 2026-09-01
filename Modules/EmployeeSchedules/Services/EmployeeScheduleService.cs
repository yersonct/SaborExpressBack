using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.DTOs;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Mappings;
using SaborExpress.Modules.EmployeeSchedules.Validators;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Modules.EmployeeSchedules.Services
{
    public class EmployeeScheduleService : IEmployeeScheduleService
    {
        private readonly IEmployeeScheduleRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IAuthRepository _authRepository;
        private readonly EmployeeScheduleValidator _validator;
        public EmployeeScheduleService(
            IEmployeeScheduleRepository repository,
            IEmployeeRepository employeeRepository,
            IBranchRepository branchRepository,
            IAuthRepository authRepository,
            EmployeeScheduleValidator validator)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _branchRepository = branchRepository;
            _authRepository = authRepository;
            _validator = validator;
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByEmployeeAsync(int employeeId)
        {
            var schedules = await _repository.GetByEmployeeAsync(employeeId);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId)
        {
            var schedules = await _repository.GetByBranchAsync(branchId);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var schedules = await _repository.GetByBranchAndDateAsync(branchId, today);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }

        public async Task<EmployeeScheduleResponseDto> CreateAsync(CreateEmployeeScheduleDto dto, int currentUserId)
        {
            _validator.ValidateCreate(dto);
            ValidateTimeRange(dto.StartTime, dto.EndTime);

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
                ?? throw new KeyNotFoundException("El empleado no existe.");

            var branch = await _branchRepository.GetByIdAsync(dto.BranchId)
                ?? throw new KeyNotFoundException("La sucursal no existe.");

            EnsureBranchAccess(currentUser, dto.BranchId);
            EnsureRoleBelongsToEmployee(employee, dto.RoleId);

            var hasOverlap = await HasOverlapAsync(dto.EmployeeId, dto.ShiftDate, dto.StartTime, dto.EndTime, excludeScheduleId: null);
            if (hasOverlap)
                throw new InvalidOperationException("El empleado ya tiene un turno asignado que se cruza en ese horario.");

            var entity = dto.ToModel();
            var created = await _repository.AddAsync(entity);

            var full = await _repository.GetByIdAsync(created.Id)
                ?? throw new InvalidOperationException("Error al crear el turno.");

            return full.ToResponseDto();
        }

        public async Task<EmployeeScheduleResponseDto> UpdateAsync(int id, UpdateEmployeeScheduleDto dto, int currentUserId)
        {
             _validator.ValidateUpdate(dto);
            ValidateTimeRange(dto.StartTime, dto.EndTime);

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var schedule = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El turno no existe.");

            EnsureBranchAccess(currentUser, schedule.BranchId);

            var employee = await _employeeRepository.GetByIdAsync(schedule.EmployeeId)
                ?? throw new KeyNotFoundException("El empleado no existe.");
            EnsureRoleBelongsToEmployee(employee, dto.RoleId);

            var hasOverlap = await HasOverlapAsync(schedule.EmployeeId, dto.ShiftDate, dto.StartTime, dto.EndTime, excludeScheduleId: id);
            if (hasOverlap)
                throw new InvalidOperationException("El empleado ya tiene un turno asignado que se cruza en ese horario.");

            schedule.RoleId = dto.RoleId;
            schedule.ShiftDate = dto.ShiftDate;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Notes = dto.Notes;

            await _repository.UpdateAsync(schedule);
            return schedule.ToResponseDto();
        }

        public async Task<EmployeeScheduleResponseDto> UpdateStatusAsync(int id, UpdateScheduleStatusDto dto, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var schedule = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El turno no existe.");

            EnsureBranchAccess(currentUser, schedule.BranchId);

            schedule.Status = dto.Status;
            await _repository.UpdateAsync(schedule);
            return schedule.ToResponseDto();
        }

        public async Task DeleteAsync(int id, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var schedule = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El turno no existe.");

            EnsureBranchAccess(currentUser, schedule.BranchId);

            await _repository.DeleteAsync(schedule);
        }

        // --- Helpers privados ---

        private static void ValidateTimeRange(TimeOnly start, TimeOnly end)
        {
            if (start >= end)
                throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");
        }

        private async Task<bool> HasOverlapAsync(int employeeId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeScheduleId)
        {
            var schedulesOfDay = await _repository.GetByEmployeeAndDateAsync(employeeId, date);

            return schedulesOfDay
                .Where(s => excludeScheduleId == null || s.Id != excludeScheduleId)
                .Any(s => start < s.EndTime && s.StartTime < end);
        }

        private static void EnsureBranchAccess(User currentUser, int? targetBranchId)
        {
            var esGerente = currentUser.HasRole(RoleNames.Gerente);
            if (esGerente) return;

            var esAdministrador = currentUser.HasRole(RoleNames.Administrador);
            if (!esAdministrador)
                throw new InvalidOperationException("No tienes permiso para gestionar turnos.");

            if (currentUser.Employee?.BranchId == null || targetBranchId != currentUser.Employee.BranchId)
                throw new InvalidOperationException("Solo puedes gestionar turnos de tu misma sede.");
        }

        private static void EnsureRoleBelongsToEmployee(Employee employee, int roleId)
        {
            var tieneEseRol = employee.User?.UserRoles.Any(ur => ur.RoleId == roleId) ?? false;

            if (!tieneEseRol)
                throw new ArgumentException("El empleado no tiene asignado ese rol. Solo se puede programar un turno con un rol que el empleado ya posee.");
        }

        public async Task<CurrentShiftResponseDto> GetCurrentShiftAsync(int employeeId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var nowTime = TimeOnly.FromDateTime(DateTime.Now);

            var schedulesToday = await _repository.GetByEmployeeAndDateAsync(employeeId, today);

            var activeSchedule = schedulesToday
                .FirstOrDefault(s => nowTime >= s.StartTime && nowTime <= s.EndTime);

            if (activeSchedule == null)
            {
                return new CurrentShiftResponseDto { HasActiveShift = false };
            }

            var fullSchedule = await _repository.GetByIdAsync(activeSchedule.Id)
                ?? throw new InvalidOperationException("Error al obtener el turno activo.");

            return new CurrentShiftResponseDto
            {
                HasActiveShift = true,
                ScheduleId = fullSchedule.Id,
                RoleId = fullSchedule.RoleId,
                RoleName = fullSchedule.Role?.Name,
                StartTime = fullSchedule.StartTime,
                EndTime = fullSchedule.EndTime
            };
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByEmployeeAsync(int employeeId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var targetEmployee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("El empleado no existe.");

            EnsureBranchAccess(currentUser, targetEmployee.BranchId);

            var schedules = await _repository.GetByEmployeeAsync(employeeId);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            EnsureBranchAccess(currentUser, branchId);

            var schedules = await _repository.GetByBranchAsync(branchId);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }

        public async Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            EnsureBranchAccess(currentUser, branchId);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var schedules = await _repository.GetByBranchAndDateAsync(branchId, today);
            return schedules.Select(s => s.ToResponseDto()).ToList();
        }
    }
}