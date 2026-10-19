using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.EmployeeSchedules.Enum;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.Auth.Helpers
{
    // Diferenciar roles ASIGNADOS de permisos EFECTIVOS evita usar los cargos
    // históricos de un empleado durante un turno de otro cargo.
    public sealed record EffectiveRoleSnapshot(
        bool HasActiveShift,
        string? ActiveShiftRoleName,
        IReadOnlyCollection<string> Roles);

    public static class EffectiveRolePolicy
    {
        private static readonly HashSet<string> OperationalRoles = new(StringComparer.Ordinal)
        {
            RoleNames.Mesero,
            RoleNames.Cajero,
            RoleNames.Cocinero,
            RoleNames.AuxiliarCocina,
            RoleNames.Repartidor
        };

        public static async Task<EffectiveRoleSnapshot> ResolveAsync(
            User user, IEmployeeScheduleRepository schedules)
        {
            var assignedRoles = user.UserRoles
                .Select(userRole => userRole.Role.Name)
                .ToHashSet(StringComparer.Ordinal);

            var isOperationalEmployee = user.Employee != null &&
                assignedRoles.Overlaps(OperationalRoles);

            // Usuarios sin roles operativos (por ejemplo un cliente o gerente)
            // no dependen del turno para sus funciones propias.
            var hasActiveShift = !isOperationalEmployee;
            string? activeShiftRoleName = null;

            if (isOperationalEmployee)
            {
                var now = ColombiaTime.Now;
                var today = DateOnly.FromDateTime(now);
                var time = TimeOnly.FromDateTime(now);
                var shifts = await schedules.GetByEmployeeAndDateAsync(user.Employee!.Id, today);

                var activeShift = shifts.FirstOrDefault(shift =>
                    shift.Status == ScheduleStatus.Programado &&
                    time >= shift.StartTime && time < shift.EndTime);

                if (activeShift != null)
                {
                    var assignedRole = user.UserRoles
                        .FirstOrDefault(role => role.RoleId == activeShift.RoleId);
                    if (assignedRole != null)
                    {
                        hasActiveShift = true;
                        activeShiftRoleName = assignedRole.Role.Name;
                    }
                }
            }

            var effectiveRoles = new HashSet<string>(StringComparer.Ordinal);

            if (assignedRoles.Contains(RoleNames.Gerente))
                effectiveRoles.Add(RoleNames.Gerente);

            // Gerente y Administrador conservan su política propia.
            // El rol ADMINISTRADOR se mantiene solo cuando el repositorio
            // confirma que es permanente o que su turno administrativo está vigente.
            if (assignedRoles.Contains(RoleNames.Administrador) &&
                await schedules.CanActAsAdministratorAsync(user.Id, ColombiaTime.Now))
                effectiveRoles.Add(RoleNames.Administrador);

            if (assignedRoles.Contains(RoleNames.Cliente))
                effectiveRoles.Add(RoleNames.Cliente);

            if (hasActiveShift && activeShiftRoleName != null &&
                OperationalRoles.Contains(activeShiftRoleName))
                effectiveRoles.Add(activeShiftRoleName);

            // Un trabajador operativo sin turno puede usar funciones de cliente,
            // pero nunca permisos de caja, mesas, cocina o reparto.
            if (isOperationalEmployee && !hasActiveShift)
                effectiveRoles.Add(RoleNames.Cliente);

            return new EffectiveRoleSnapshot(
                hasActiveShift, activeShiftRoleName, effectiveRoles.ToArray());
        }
    }
}
