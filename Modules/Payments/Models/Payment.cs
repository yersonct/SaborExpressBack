// Modules/Payments/Models/Payment.cs
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

        // Opcional: null cuando el pago lo hizo el cliente vía Wompi,
        // sin que ningún empleado lo registre.
        public int? CashierId { get; set; } // FK
        public Employee? Cashier { get; set; }

        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        // Referencia que generamos nosotros y mandamos a Wompi;
        // se usa para cruzar con el webhook que responde.
        public string? WompiReference { get; set; }

        // Id de transacción que Wompi asigna una vez procesa el pago.
        public string? WompiTransactionId { get; set; }
    }
}