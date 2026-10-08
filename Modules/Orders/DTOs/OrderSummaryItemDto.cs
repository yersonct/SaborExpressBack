using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    // Versión liviana de un item, solo para la lista de pedidos.
    // Para el detalle completo (con historial de quién modificó, etc.)
    // se sigue usando OrderDetailResponseDto vía GET /api/Orders/{id}.
    public class OrderSummaryItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
        public bool IsToGo { get; set; }
        public int BatchNumber { get; set; }
        public OrderDetailStatus Status { get; set; }
    }
}