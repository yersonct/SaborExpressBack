// Modules/Orders/Models/OrderStatusHistory.cs
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

        // CAMBIADO: opcional — si el pedido lo creó un Cliente desde la app,
        // este primer registro del historial no tiene empleado responsable.
        public int? ChangedByEmployeeId { get; set; }
        public Employee? ChangedByEmployee { get; set; }

        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}