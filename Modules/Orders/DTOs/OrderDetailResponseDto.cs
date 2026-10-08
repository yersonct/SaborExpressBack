// Modules/Orders/DTOs/OrderDetailResponseDto.cs
using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class OrderDetailResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string? Notes { get; set; }
        public bool IsToGo { get; set; }
        public int BatchNumber { get; set; }
        public OrderDetailStatus Status { get; set; }
        public int LastModifiedByEmployeeId { get; set; }
        public string? LastModifiedByEmployeeName { get; set; }
    }
}