using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.Models
{
    // Proyeccion liviana: solo lo que Deliveries necesita para validar,
    // sin traer Customer/Employee/Table/Branch/OrderDetails completos.
    public class OrderDeliveryInfo
    {
        public int Id { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public int? CustomerId { get; set; }
        public int BranchId { get; set; }
        public int? AddressId { get; set; } // 👈 NUEVO
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}