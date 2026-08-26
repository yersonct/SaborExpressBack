using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Shared.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IEmployeeScheduleRepository _scheduleRepository;
        private readonly IEmployeeRepository _employeeRepository;

        private static readonly string[] RolesExentosDeTurno =
        {
            RoleNames.Gerente,
            RoleNames.Administrador
        };

        public AuthorizationService(
            IRolePermissionService rolePermissionService,
            IEmployeeScheduleRepository scheduleRepository,
            IEmployeeRepository employeeRepository)
        {
            _rolePermissionService = rolePermissionService;
            _scheduleRepository = scheduleRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<bool> CanPerformActionAsync(int employeeId, string permissionName)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee?.User == null)
                return false;

            // 1. Gerente/Administrador: se valida el permiso contra CUALQUIERA de sus roles,
            //    no necesitan turno activo
            if (EsRolExento(employee))
                return await AlgunRolTienePermisoAsync(employee, permissionName);

            // 2. El resto de roles: el permiso se valida SOLO contra el rol de su turno activo,
            //    no contra "cualquiera de sus roles". Sin turno activo, no hay permiso operativo.
            var activeRoleId = await GetActiveShiftRoleIdAsync(employeeId);
            if (activeRoleId == null)
                return false;

            var permisosDelRolActivo = await _rolePermissionService.GetByRoleIdAsync(activeRoleId.Value);
            return permisosDelRolActivo.Any(p => p.PermissionName == permissionName);
        }

        private async Task<bool> AlgunRolTienePermisoAsync(Employee employee, string permissionName)
        {
            foreach (var userRole in employee.User!.UserRoles)
            {
                var permisosDelRol = await _rolePermissionService.GetByRoleIdAsync(userRole.RoleId);
                if (permisosDelRol.Any(p => p.PermissionName == permissionName))
                    return true;
            }

            return false;
        }

        private async Task<int?> GetActiveShiftRoleIdAsync(int employeeId)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            var schedulesToday = await _scheduleRepository.GetByEmployeeAndDateAsync(employeeId, today);

            var activeSchedule = schedulesToday
                .FirstOrDefault(s => nowTime >= s.StartTime && nowTime <= s.EndTime);

            return activeSchedule?.RoleId;
        }

        private static bool EsRolExento(Employee employee)
        {
            var roles = employee.User?.UserRoles.Select(ur => ur.Role.Name)
                ?? Enumerable.Empty<string>();

            return roles.Any(r => RolesExentosDeTurno.Contains(r));
        }
    }
}