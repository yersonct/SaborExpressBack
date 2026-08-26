// Modules/Orders/DTOs/UpdateOrderDetailDto.cs
namespace SaborExpress.Modules.Orders.DTOs
{
    public class UpdateOrderDetailDto
    {
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }
}