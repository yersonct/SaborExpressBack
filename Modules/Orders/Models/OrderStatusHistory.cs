// SaborExpress.Modules.Orders.Models.OrderStatusHistory
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Modules.Orders.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // FK
        public Order Order { get; set; } = null!;

        public int ChangedByEmployeeId { get; set; } // FK
        public Employee ChangedByEmployee { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}