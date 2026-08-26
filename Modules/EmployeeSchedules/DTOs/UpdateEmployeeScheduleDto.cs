namespace SaborExpress.Modules.EmployeeSchedules.DTOs
{
    public class UpdateEmployeeScheduleDto
    {
        public int RoleId { get; set; }
        public DateOnly ShiftDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Notes { get; set; }
    }
}