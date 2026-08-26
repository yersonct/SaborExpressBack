// Modules/Deliveries/DTOs/AvailableOrderDto.cs
namespace SaborExpress.Modules.Deliveries.DTOs
{
    public class AvailableOrderDto
    {
        public int OrderId { get; set; }
        public int BranchId { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}