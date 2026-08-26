// Modules/Deliveries/Models/Delivery.cs
using SaborExpress.Modules.Addresses.Models;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Deliveries.Models
{
    public class Delivery
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // FK
        public Order Order { get; set; } = null!;

        public int AddressId { get; set; } // FK
        public Address Address { get; set; } = null!;

        public int DeliveryPersonId { get; set; } // FK -> Employee
        public Employee DeliveryPerson { get; set; } = null!;

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
    }
}