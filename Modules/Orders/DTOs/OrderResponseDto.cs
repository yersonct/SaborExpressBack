// Modules/Orders/DTOs/OrderResponseDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    // Para GET /api/Orders/{id} - detalle completo con sus líneas
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int? TableId { get; set; }
        public int? TableNumber { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderDetailResponseDto> OrderDetails { get; set; } = new();
    }
}