    using Microsoft.Extensions.Configuration;
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
    using SaborExpress.Modules.Roles.Interfaces;
    using SaborExpress.Modules.UserRoles.Interfaces;
    using SaborExpress.Shared.Helpers;
    using SaborExpress.Shared.Interfaces;
    using SaborExpress.Modules.Configurations.Interfaces;

    namespace SaborExpress.Modules.EmployeeSchedules.Services
    {
        public class EmployeeScheduleService : IEmployeeScheduleService
        {
            private readonly IEmployeeScheduleRepository _repository;
            private readonly IEmployeeRepository _employeeRepository;
            private readonly IBranchRepository _branchRepository;
            private readonly IAuthRepository _authRepository;
            private readonly EmployeeScheduleValidator _validator;
            private readonly IRoleRepository _roleRepository;
            private readonly IUserRoleRepository _userRoleRepository;
            private readonly IRefreshTokenRepository _refreshTokenRepository;
              private readonly IRealtimeNotifier _realtime;
            private readonly IBranchOperationalSettingsRepository _settingsRepository;
            private readonly string? _initialManagerEmail;

            public EmployeeScheduleService(
                IEmployeeScheduleRepository repository,
                IEmployeeRepository employeeRepository,
                IBranchRepository branchRepository,
                IAuthRepository authRepository,
                EmployeeScheduleValidator validator,
                IRoleRepository roleRepository,
                IUserRoleRepository userRoleRepository,
                IRefreshTokenRepository refreshTokenRepository,
                IRealtimeNotifier realtime,
                IBranchOperationalSettingsRepository settingsRepository,
                IConfiguration configuration)
            {
                _settingsRepository = settingsRepository;
                _initialManagerEmail = configuration["InitialManager:Email"]?.Trim();
                _repository = repository;
                _employeeRepository = employeeRepository;
                _branchRepository = branchRepository;
                _authRepository = authRepository;
                _validator = validator;
                _roleRepository = roleRepository;
                _userRoleRepository = userRoleRepository;
                _refreshTokenRepository = refreshTokenRepository;
                _realtime = realtime;
            }

            public async Task<List<EmployeeScheduleResponseDto>> GetByEmployeeAsync(int employeeId)
            {
                await SynchronizeEmployeeScheduleHoursAsync(employeeId);
                var schedules = await _repository.GetByEmployeeAsync(employeeId);
                return schedules.Select(s => s.ToResponseDto()).ToList();
            }

            public async Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId)
            {
                await SynchronizeBranchHoursAsync(branchId);
                var schedules = await _repository.GetByBranchAsync(branchId);
                return schedules.Select(s => s.ToResponseDto()).ToList();
            }

            // Roles que solo tienen sentido mientras el empleado tenga un turno vigente
            // (HOY o en el futuro) que los justifique. Gerente/Administrador/Cliente
            // NUNCA se tocan aquí — no se otorgan ni se revocan por turno.
        // Genérico para cualquier rol, incluido Gerente/Administrador: la única
    // pregunta es si ESE rol específico le llegó al empleado a través de un
    // turno. Si nunca hubo un turno con ese RoleId, el rol no se toca aquí
    // (ej. el Gerente inicial sembrado por InitialManagerSeeder, que nunca
    // tuvo un EmployeeSchedule de por medio).
public async Task RevokeExpiredRolesAsync()
{
    var today = DateOnly.FromDateTime(ColombiaTime.Now); // antes: DateTime.UtcNow (desfasaba el día en las noches, Colombia UTC-5)
    var employeeIds = await _repository.GetEmployeeIdsWithSchedulesAsync();
    var rolesWereRevoked = false;

        foreach (var employeeId in employeeIds)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee?.User == null)
                continue;

            // Roles que este empleado alguna vez tuvo por un turno (pasado o futuro)
            var everAssignedByShift = await _repository.GetAllRoleIdsEverAssignedAsync(employeeId);

            // Roles que necesita HOY o en el futuro
            var requiredRoleIds = await _repository.GetRequiredRoleIdsAsync(employeeId, today);

            // Se revoca un rol solo si (a) alguna vez vino de un turno, y
            // (b) ningún turno de hoy en adelante lo sigue justificando.
            var toRevoke = employee.User.UserRoles
                .Where(ur => everAssignedByShift.Contains(ur.RoleId) && !requiredRoleIds.Contains(ur.RoleId))
                .ToList();

            if (toRevoke.Count == 0)
                continue;

            foreach (var ur in toRevoke)
                employee.User.UserRoles.Remove(ur);

            await _employeeRepository.UpdateAsync(employee);
            rolesWereRevoked = true;
            await ForceLogoutUserAsync(employee.UserId, "role-expired");
        }

        if (rolesWereRevoked)
            await _realtime.AdminDataChangedAsync("Employees");
    }
            public async Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId)
            {
                var today = DateOnly.FromDateTime(ColombiaTime.Now);
                await SynchronizeBranchHoursAsync(branchId);
                var schedules = await _repository.GetByBranchAndDateAsync(branchId, today);
                return schedules.Select(s => s.ToResponseDto()).ToList();
            }

            public async Task<EmployeeScheduleResponseDto> CreateAsync(CreateEmployeeScheduleDto dto, int currentUserId)
            {
                _validator.ValidateCreate(dto);

                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId)
                    ?? throw new KeyNotFoundException("El empleado no existe.");

                var branch = await _branchRepository.GetByIdAsync(dto.BranchId)
                    ?? throw new KeyNotFoundException("La sucursal no existe.");

                    EnsureBranchAccess(currentUser, dto.BranchId);
                var branchHours = await GetBranchHoursAsync(dto.BranchId);
                dto.StartTime = branchHours.open;
                dto.EndTime = branchHours.close;
                ValidateTimeRange(dto.StartTime, dto.EndTime);
                var alreadyHadRole = employee.User?.UserRoles.Any(ur => ur.RoleId == dto.RoleId) == true;
                var branchChanged = employee.BranchId == null;
                await EnsureEmployeeHasRoleAsync(employee, dto.RoleId, currentUser, dto.BranchId);

                // Igual que el rol: si el empleado no tiene sede todavía, nace
                // con la del primer turno que se le asigna.
                if (branchChanged)
                {
                    employee.BranchId = dto.BranchId;
                    await _employeeRepository.UpdateAsync(employee);
                }

                var hasOverlap = await HasOverlapAsync(dto.EmployeeId, dto.ShiftDate, dto.StartTime, dto.EndTime, excludeScheduleId: null);
                if (hasOverlap)
                    throw new InvalidOperationException("El empleado ya tiene un turno asignado que se cruza en ese horario.");

                var entity = dto.ToModel();
                var created = await _repository.AddAsync(entity);
                if (branchChanged)
                    await ForceLogoutEmployeeAsync(dto.EmployeeId, "branch-changed");
                else if (!alreadyHadRole)
                    await ForceLogoutEmployeeAsync(dto.EmployeeId, "role-changed");
                else
                    await ForceRelogInIfTodayAsync(dto.EmployeeId, currentUserId, dto.ShiftDate);

                var full = await _repository.GetByIdAsync(created.Id)
                    ?? throw new InvalidOperationException("Error al crear el turno.");

                return ToResponseDto(full, currentUser);
            }

            public async Task<int> TransferEmployeeToBranchAsync(int employeeId, int newBranchId, int currentUserId)
            {
                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                // Trasladar entre sedes es una decisión de nivel red, no de una sola sede:
                // solo el Gerente puede hacerlo.
                if (!currentUser.HasRole(RoleNames.Gerente))
                    throw new InvalidOperationException("Solo el Gerente puede trasladar un empleado de sede.");

                var employee = await _employeeRepository.GetByIdAsync(employeeId)
                    ?? throw new KeyNotFoundException("El empleado no existe.");
                var branchChanged = employee.BranchId != newBranchId;

                var newBranch = await _branchRepository.GetByIdAsync(newBranchId)
                    ?? throw new KeyNotFoundException("La sucursal destino no existe.");

                var today = DateOnly.FromDateTime(ColombiaTime.Now);
                var allSchedules = await _repository.GetByEmployeeAsync(employeeId);

                // Solo se trasladan turnos futuros (hoy inclusive). Los pasados se quedan
                // donde realmente ocurrieron, para no falsear el historial.
                var toTransfer = allSchedules.Where(s => s.ShiftDate >= today).ToList();

                foreach (var schedule in toTransfer)
                    EnsureCanModifySchedule(currentUser, schedule);

                foreach (var schedule in toTransfer)
                {
                    schedule.BranchId = newBranchId;
                    await _repository.UpdateAsync(schedule);
                }

                employee.BranchId = newBranchId;
                await _employeeRepository.UpdateAsync(employee);

                if (branchChanged)
                    await ForceLogoutEmployeeAsync(employeeId, "branch-changed");
                return toTransfer.Count;
            }

        public async Task<EmployeeScheduleResponseDto> UpdateAsync(int id, UpdateEmployeeScheduleDto dto, int currentUserId)
        {
            _validator.ValidateUpdate(dto);

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var schedule = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El turno no existe.");

            EnsureBranchAccess(currentUser, schedule.BranchId);
            var requestedRole = await _roleRepository.GetByIdAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("El rol especificado no existe.");
            EnsureCanModifySchedule(currentUser, schedule);

            var employee = await _employeeRepository.GetByIdAsync(schedule.EmployeeId)
                ?? throw new KeyNotFoundException("El empleado no existe.");
            var roleChanged = schedule.RoleId != dto.RoleId;
            var effectiveBranchId = dto.BranchId != 0 ? dto.BranchId : schedule.BranchId;
            if (!currentUser.HasRole(RoleNames.Gerente) &&
                roleChanged && IsManagementRoleName(schedule.Role?.Name))
                throw new InvalidOperationException("No tienes permiso para gestionar turnos.");

            await EnsureEmployeeHasRoleAsync(employee, dto.RoleId, currentUser, effectiveBranchId);

            var branchHours = await GetBranchHoursAsync(effectiveBranchId);
            dto.StartTime = branchHours.open;
            dto.EndTime = branchHours.close;
            ValidateTimeRange(dto.StartTime, dto.EndTime);

            var hasOverlap = await HasOverlapAsync(schedule.EmployeeId, dto.ShiftDate, dto.StartTime, dto.EndTime, excludeScheduleId: id);
            if (hasOverlap)
                throw new InvalidOperationException("El empleado ya tiene un turno asignado que se cruza en ese horario.");

            // Si el rol cambia con esta edición, el rol ANTERIOR podría quedar
            // huérfano: si ningún otro turno restante lo justifica, hay que
            // revocarlo, igual que ya se hace en DeleteAsync. Sin esto, un empleado
            // podía quedarse con un rol (ej. Administrador) que ya no corresponde
            // a ningún turno real, simplemente por haber editado el turno que
            // originalmente se lo otorgó.
            var previousRoleId = schedule.RoleId;
            var previousDate = schedule.ShiftDate;
            var branchChanged = dto.BranchId != 0 && dto.BranchId != schedule.BranchId;
            if (branchChanged)
            {
                EnsureBranchAccess(currentUser, dto.BranchId);
                var newBranch = await _branchRepository.GetByIdAsync(dto.BranchId)
                    ?? throw new KeyNotFoundException("La sucursal no existe.");
                schedule.BranchId = dto.BranchId;
                schedule.Branch = newBranch;
            }

            schedule.RoleId = dto.RoleId;
            schedule.ShiftDate = dto.ShiftDate;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Notes = dto.Notes;

            // Si el rol cambió, la navegación schedule.Role sigue apuntando al
            // rol ANTERIOR en memoria (EF no la refresca sola al cambiar el FK).
            // La reasignamos explícitamente para que la respuesta muestre el rol nuevo.
            if (roleChanged)
            {
                schedule.Role = requestedRole;
            }

            await _repository.UpdateAsync(schedule);

            if (roleChanged)
            {
                var remainingSchedules = await _repository.GetByEmployeeAsync(schedule.EmployeeId);
                await RemoveRoleIfNoMoreSchedulesAsync(schedule.EmployeeId, previousRoleId, remainingSchedules);
            }

            if (roleChanged)
                await ForceLogoutEmployeeAsync(schedule.EmployeeId, "role-changed");
            else if (branchChanged)
                await ForceLogoutEmployeeAsync(schedule.EmployeeId, "branch-changed");
            else if (previousDate != schedule.ShiftDate)
                await ForceRelogInIfTodayAsync(schedule.EmployeeId, currentUserId, previousDate, schedule.ShiftDate);

            return ToResponseDto(schedule, currentUser);
        }
            public async Task<EmployeeScheduleResponseDto> UpdateStatusAsync(int id, UpdateScheduleStatusDto dto, int currentUserId)
            {
                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                var schedule = await _repository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("El turno no existe.");

                EnsureBranchAccess(currentUser, schedule.BranchId);
                EnsureCanModifySchedule(currentUser, schedule);

                schedule.Status = dto.Status;
                await _repository.UpdateAsync(schedule);
                return ToResponseDto(schedule, currentUser);
            }

            public async Task DeleteAsync(int id, int currentUserId)
            {
                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                var schedule = await _repository.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("El turno no existe.");

                EnsureBranchAccess(currentUser, schedule.BranchId);
                EnsureCanModifySchedule(currentUser, schedule);
                if (!currentUser.HasRole(RoleNames.Gerente) &&
                    IsManagementRoleName(schedule.Role?.Name))
                    throw new InvalidOperationException("No tienes permiso para gestionar turnos.");

                var employeeId = schedule.EmployeeId;
                var roleId = schedule.RoleId;
var shiftDate = schedule.ShiftDate;

                await _repository.DeleteAsync(schedule);

                var remainingSchedules = await _repository.GetByEmployeeAsync(employeeId);

                // Simétrico al "nacen juntos" de EnsureEmployeeHasRoleAsync:
                // si este era el ÚLTIMO turno del empleado con este rol, el rol
                // se retira junto con él. Si le quedan otros turnos con ese
                // mismo rol, no se lo quitamos.
                var roleRemoved = await RemoveRoleIfNoMoreSchedulesAsync(employeeId, roleId, remainingSchedules);

                // Si ya no le queda NINGÚN turno (de ningún rol), tampoco tiene
                // sentido que conserve la sede — se la quitamos también.
                var branchRemoved = false;
                if (remainingSchedules.Count == 0)
                {
                    var employee = await _employeeRepository.GetByIdAsync(employeeId);
                    if (employee != null && employee.BranchId != null)
                    {
                        employee.BranchId = null;
                        await _employeeRepository.UpdateAsync(employee);
                        branchRemoved = true;
                    }
                }

                if (roleRemoved || branchRemoved)
                    await ForceLogoutEmployeeAsync(
                        employeeId,
                        branchRemoved ? "branch-changed" : "role-changed");
                else
                    await ForceRelogInIfTodayAsync(employeeId, currentUserId, shiftDate);
            }

            private async Task<bool> RemoveRoleIfNoMoreSchedulesAsync(
                int employeeId, int roleId, List<Models.EmployeeSchedule> remainingSchedules)
            {
                var stillHasThisRole = remainingSchedules.Any(s => s.RoleId == roleId);
                if (stillHasThisRole)
                    return false;

                var employee = await _employeeRepository.GetByIdAsync(employeeId);
                if (employee?.User == null)
                    return false;

                var userRole = employee.User.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
                if (userRole == null) return false;

                employee.User.UserRoles.Remove(userRole);
                await _employeeRepository.UpdateAsync(employee);
                return true;
            }

            // Si el cambio toca el turno de HOY, el empleado debe volver a iniciar
            // sesión: se revocan sus refresh-tokens y se le avisa en tiempo real.
            // Quien hace el cambio no se saca a sí mismo.
            private async Task ForceRelogInIfTodayAsync(int employeeId, int actorUserId, params DateOnly[] dates)
            {
                var today = DateOnly.FromDateTime(ColombiaTime.Now);
                if (!dates.Contains(today)) return;

                var employee = await _employeeRepository.GetByIdAsync(employeeId);
                if (employee == null || employee.UserId == actorUserId) return;

                await ForceLogoutUserAsync(employee.UserId, "shift-changed");
            }

            private async Task ForceLogoutEmployeeAsync(
                int employeeId, string reason)
            {
                var employee = await _employeeRepository.GetByIdAsync(employeeId);
                if (employee == null) return;

                await ForceLogoutUserAsync(employee.UserId, reason);
            }

            private async Task ForceLogoutUserAsync(int userId, string reason)
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(userId);
                await _authRepository.ClearSessionTokensAsync(new[] { userId });
                await _realtime.ForceLogoutAsync(userId, reason);
            }

            // --- Helpers privados ---
            
            // La configuración de la sede define las horas exactas de cada turno.
            private async Task SynchronizeBranchHoursAsync(int branchId)
            {
                var settings = await _settingsRepository.GetByBranchIdAsync(branchId);
                if (settings is null) return;

                var changed = await _repository.ApplyBranchHoursToUpcomingAsync(
                    branchId,
                    DateOnly.FromDateTime(ColombiaTime.Now),
                    settings.OpeningTime,
                    settings.ClosingTime);

                if (changed)
                {
                    await _realtime.AdminDataChangedAsync("EmployeeSchedules");
                    await _realtime.AdminDataChangedAsync("Employees");
                }
            }

            private async Task SynchronizeEmployeeScheduleHoursAsync(int employeeId)
            {
                var today = DateOnly.FromDateTime(ColombiaTime.Now);
                var schedules = await _repository.GetByEmployeeAsync(employeeId);
                var branchIds = schedules
                    .Where(schedule => schedule.ShiftDate >= today)
                    .Select(schedule => schedule.BranchId)
                    .Distinct();

                foreach (var branchId in branchIds)
                    await SynchronizeBranchHoursAsync(branchId);
            }

            private async Task<(TimeOnly open, TimeOnly close)> GetBranchHoursAsync(int branchId)
            {
                var settings = await _settingsRepository.GetByBranchIdAsync(branchId);
                if (settings is null)
                    throw new InvalidOperationException(
                        "La sede no tiene hora de apertura y cierre configuradas. " +
                        "Configúralas en Configuración > Ajustes del Restaurante antes de crear turnos.");

                return (settings.OpeningTime, settings.ClosingTime);
            }

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

            private async Task EnsureEmployeeHasRoleAsync(
                Employee employee,
                int roleId,
                User currentUser,
                int branchId)
            {
                if (employee.User == null)
                    throw new InvalidOperationException("El empleado no tiene una cuenta de usuario asociada.");

                var role = await _roleRepository.GetByIdAsync(roleId)
                    ?? throw new ArgumentException("El rol especificado no existe.");

                if (!currentUser.HasRole(RoleNames.Gerente) && IsManagementRoleName(role.Name))
                    throw new InvalidOperationException("No tienes permiso para asignar roles.");

                var tieneEseRol = employee.User.UserRoles.Any(ur => ur.RoleId == roleId);
                if (tieneEseRol)
                    return;

                if (role.Name == RoleNames.Administrador &&
                    await _userRoleRepository.ExistsAdministradorInBranchAsync(
                        branchId, excludeUserId: employee.User.Id))
                    throw new InvalidOperationException("Esta sede ya tiene un Administrador asignado.");

                var tieneCv = employee.CvFile != null && employee.CvFile.Length > 0;
                if (role.RequiresCv && !tieneCv)
                    throw new InvalidOperationException(
                        $"El rol {role.Name} requiere hoja de vida (CV). Sube el CV del empleado antes de asignarle este turno.");

                // Primer turno con este rol: el turno y el rol nacen juntos.
                // Antes de esto, el empleado no tenía ningún permiso real en el sistema.
                employee.User.UserRoles.Add(new SaborExpress.Modules.UsersRoles.Models.UserRole
                {
                    UserId = employee.User.Id,
                    RoleId = roleId
                });

                await _employeeRepository.UpdateAsync(employee);
            }

            private static bool IsManagementRoleName(string? roleName) =>
                roleName == RoleNames.Gerente || roleName == RoleNames.Administrador;

            private EmployeeScheduleResponseDto ToResponseDto(
                Models.EmployeeSchedule schedule,
                User currentUser)
            {
                var response = schedule.ToResponseDto();
                response.IsOwnLockedShift = IsOwnShiftLockedForManager(currentUser, schedule);
                return response;
            }

            private void EnsureCanModifySchedule(
                User currentUser,
                Models.EmployeeSchedule schedule)
            {
                if (IsOwnShiftLockedForManager(currentUser, schedule))
                {
                    throw new InvalidOperationException(
                        "No puedes editar ni eliminar tus propios turnos como Gerente o Administrador.");
                }
            }

            private bool IsOwnShiftLockedForManager(
                User currentUser,
                Models.EmployeeSchedule schedule)
            {
                if (IsInitialManager(currentUser) || schedule.Employee?.UserId != currentUser.Id)
                    return false;

                return currentUser.HasRole(RoleNames.Gerente) ||
                    currentUser.HasRole(RoleNames.Administrador);
            }

            private bool IsInitialManager(User user) =>
                !string.IsNullOrWhiteSpace(_initialManagerEmail) &&
                string.Equals(
                    user.Email?.Trim(),
                    _initialManagerEmail,
                    StringComparison.OrdinalIgnoreCase);

            public async Task<CurrentShiftResponseDto> GetCurrentShiftAsync(int employeeId)
            {
                var now = ColombiaTime.Now;
                var today = DateOnly.FromDateTime(now);
                var nowTime = TimeOnly.FromDateTime(now);

                await SynchronizeEmployeeScheduleHoursAsync(employeeId);
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

                await SynchronizeEmployeeScheduleHoursAsync(employeeId);
                var schedules = await _repository.GetByEmployeeAsync(employeeId);
                return schedules.Select(s => ToResponseDto(s, currentUser)).ToList();
            }

            public async Task<List<EmployeeScheduleResponseDto>> GetByBranchAsync(int branchId, int currentUserId)
            {
                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                EnsureBranchAccess(currentUser, branchId);

                await SynchronizeBranchHoursAsync(branchId);
                var schedules = await _repository.GetByBranchAsync(branchId);
                return schedules.Select(s => ToResponseDto(s, currentUser)).ToList();
            }

            public async Task<List<EmployeeScheduleResponseDto>> GetByBranchTodayAsync(int branchId, int currentUserId)
            {
                var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                    ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

                EnsureBranchAccess(currentUser, branchId);

                var today = DateOnly.FromDateTime(ColombiaTime.Now);
                await SynchronizeBranchHoursAsync(branchId);
                var schedules = await _repository.GetByBranchAndDateAsync(branchId, today);
                return schedules.Select(s => ToResponseDto(s, currentUser)).ToList();
            }
        }
    }
