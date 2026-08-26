namespace SaborExpress.Modules.EmployeeSchedules.DTOs
{
    public class CurrentShiftResponseDto
    {
        public bool HasActiveShift { get; set; }
        public int? ScheduleId { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }
}