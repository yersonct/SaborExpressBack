// Modules/Orders/DTOs/OrderDetailHistoryResponseDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class OrderDetailHistoryResponseDto
    {
        public int Id { get; set; }
        public int OrderDetailId { get; set; }
        public OrderDetailHistoryAction Action { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Reason { get; set; }
        public int ChangedByEmployeeId { get; set; }
        public string? ChangedByEmployeeName { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}