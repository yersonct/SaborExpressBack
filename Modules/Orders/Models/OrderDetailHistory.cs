// SaborExpress.Modules.Orders.Models.OrderDetailHistory
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Modules.Orders.Models
{
    public class OrderDetailHistory
    {
        public int Id { get; set; }

        public int OrderDetailId { get; set; } // FK
        public OrderDetail OrderDetail { get; set; } = null!;

        public int? ChangedByEmployeeId { get; set; } // FK
        public Employee ChangedByEmployee { get; set; } = null!;

        public OrderDetailHistoryAction Action { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Reason { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}