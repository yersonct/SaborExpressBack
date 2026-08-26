// Modules/Orders/DTOs/OrderSummaryDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    // Para listados (GetAll, por mesa, por cliente, por sucursal) - sin las líneas del pedido
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int? TableId { get; set; }
        public int? TableNumber { get; set; }
        public int BranchId { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}