using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.EmployeeSchedules.Enum;

namespace SaborExpress.Modules.EmployeeSchedules.Models
{
    public class EmployeeSchedule
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // Nuevo: con que rol trabaja el empleado en este turno puntual
        // (debe ser uno de los roles que el empleado ya tiene asignados en UserRoles)
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateOnly ShiftDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public bool ReminderSent { get; set; } = false;

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Programado;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}