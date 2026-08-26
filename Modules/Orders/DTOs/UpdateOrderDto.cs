// Modules/Orders/DTOs/UpdateOrderDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class UpdateOrderDto
    {
        public int? TableId { get; set; }
        public OrderType OrderType { get; set; }
        public string? Notes { get; set; }
    }
}