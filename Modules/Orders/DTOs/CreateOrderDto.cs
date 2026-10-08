// Modules/Orders/DTOs/CreateOrderDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class CreateOrderDto
    {
        public int? CustomerId { get; set; }
        public int? TableId { get; set; }

        // Ahora opcional: obligatorio para todo excepto Delivery,
        // donde se calcula internamente a partir de AddressId.
        public int? BranchId { get; set; }

        public int? AddressId { get; set; }

        public OrderType OrderType { get; set; }
        public string? Notes { get; set; }
        public string? GuestName { get; set; }
    }
}