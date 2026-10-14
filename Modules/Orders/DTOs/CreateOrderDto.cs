// Modules/Orders/DTOs/CreateOrderDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class CreateOrderDto
    {
        public int? CustomerId { get; set; }
        public int? TableId { get; set; }

        // Delivery usa la sede más cercana cuando se omite; la app de cliente
        // puede enviar otra sede y el servidor valida que esté activa y cubra la dirección.
        public int? BranchId { get; set; }

        public int? AddressId { get; set; }

        public OrderType OrderType { get; set; }
        public string? Notes { get; set; }
        public string? GuestName { get; set; }
    }
}
