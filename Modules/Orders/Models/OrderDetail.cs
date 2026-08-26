// Modules/Orders/Models/OrderDetail.cs
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Products.Models;

namespace SaborExpress.Modules.Orders.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // FK
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; } // FK
        public Product Product { get; set; } = null!;

        public int LastModifiedByEmployeeId { get; set; } // FK
        public Employee LastModifiedByEmployee { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string? Notes { get; set; }
        public OrderDetailStatus Status { get; set; } = OrderDetailStatus.Pending;

        public ICollection<OrderDetailHistory> Histories { get; set; } = new List<OrderDetailHistory>();
    }
}