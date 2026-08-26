// Modules/Orders/DTOs/OrderStatusHistoryResponseDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class OrderStatusHistoryResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public int ChangedByEmployeeId { get; set; }
        public string? ChangedByEmployeeName { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}