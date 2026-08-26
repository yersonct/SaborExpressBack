// SaborExpress.Modules.Payments.Models.Payment
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Modules.Payments.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // FK
        public Order Order { get; set; } = null!;

        public int CashierId { get; set; } // FK
        public Employee Cashier { get; set; } = null!;

        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}