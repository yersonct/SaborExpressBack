// Modules/Orders/DTOs/CreateOrderDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    // EmployeeId sale del usuario autenticado, no del body.
    public class CreateOrderDto
    {
        public int? CustomerId { get; set; }
        public int? TableId { get; set; }
        public int BranchId { get; set; }
        public OrderType OrderType { get; set; }
        public string? Notes { get; set; }
    }
}