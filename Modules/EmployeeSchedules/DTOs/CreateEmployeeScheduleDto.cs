namespace SaborExpress.Modules.EmployeeSchedules.DTOs
{
    public class CreateEmployeeScheduleDto
    {
        public int EmployeeId { get; set; }
        public int BranchId { get; set; }
        public int RoleId { get; set; }
        public DateOnly ShiftDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Notes { get; set; }
    }
}