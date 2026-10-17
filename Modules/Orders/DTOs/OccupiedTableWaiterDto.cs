namespace SaborExpress.Modules.Orders.DTOs
{
    public class OccupiedTableWaiterDto
    {
        public int TableId { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
    }
}
