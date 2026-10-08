// Modules/Orders/Models/Order.cs
using System.ComponentModel.DataAnnotations.Schema; 
using SaborExpress.Modules.Addresses.Models;
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Modules.Orders.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int? TableId { get; set; }
        public Table? Table { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // NUEVO: dirección de entrega. Obligatoria solo cuando OrderType es domicilio;
        // null para pedidos DineIn o ToGo desde el mostrador.
        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public OrderType OrderType { get; set; }
        public OrderChannel Channel { get; set; } = OrderChannel.Mostrador;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        
        [NotMapped]
        public int? AssignedDeliveryPersonId { get; set; }

        [NotMapped]
        public string? AssignedDeliveryPersonName { get; set; }
        public string? GuestName { get; set; } // Nombre libre cuando no hay Customer vinculado (ej. pedidos ToGo)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}