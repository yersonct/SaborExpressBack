// Modules/Orders/DTOs/CreateOrderDetailDto.cs
namespace SaborExpress.Modules.Orders.DTOs
{
    // El mesero solo manda producto, cantidad y notas.
    // OrderId viene de la ruta, UnitPrice se calcula del catálogo (no se confía en el cliente).
    public class CreateOrderDetailDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public bool IsToGo { get; set; } = false;
    }
}