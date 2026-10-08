// Modules/Orders/DTOs/OrderSummaryDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? EmployeeId { get; set; }        // CAMBIADO: ahora int?
        public string? EmployeeName { get; set; }
        public int? TableId { get; set; }
        public int? TableNumber { get; set; }
        public int BranchId { get; set; }

        public int? AssignedDeliveryPersonId { get; set; }
        public string? AssignedDeliveryPersonName { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public bool IsFullyPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderSummaryItemDto> OrderDetails { get; set; } = new(); // 👈 nuevo
    }
}