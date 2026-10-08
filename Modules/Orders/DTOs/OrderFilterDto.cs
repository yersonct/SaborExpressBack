// Modules/Orders/DTOs/OrderFilterDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    // Filtros para GET /api/Orders (Gerente/Admin/Cajero)
    public class OrderFilterDto
    {
        public int? BranchId { get; set; }
        public OrderStatus? Status { get; set; }
         public int? EmployeeId { get; set; }
    }
}