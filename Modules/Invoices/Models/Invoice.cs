// Modules/Invoices/Models/Invoice.cs
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Modules.Invoices.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public int PaymentId { get; set; } // FK - 1 a 1 con Payment
        public Payment Payment { get; set; } = null!;

        public int OrderId { get; set; } // FK - denormalizado para consultas rápidas
        public Order Order { get; set; } = null!;

        public int BranchId { get; set; } // FK - dueña de la numeración consecutiva
        public Branch Branch { get; set; } = null!;

        public string InvoiceNumber { get; set; } = string.Empty; // ej: SUC001-000123
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public bool IsVoided { get; set; } = false;
        public string? VoidReason { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    }
}
