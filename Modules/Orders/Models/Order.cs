// Modules/Orders/Models/Order.cs
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Modules.Orders.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // CAMBIADO: ahora es opcional. Un pedido creado por un Cliente desde
        // la app empieza SIN empleado — se asigna después, cuando un Mesero
        // o Repartidor realmente lo toma/atiende.
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int? TableId { get; set; }
        public Table? Table { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public OrderType OrderType { get; set; }

        public OrderChannel Channel { get; set; } = OrderChannel.Mostrador;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    }
}