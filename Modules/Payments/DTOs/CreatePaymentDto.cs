// Modules/Payments/DTOs/CreatePaymentDto.cs
using SaborExpress.Modules.Payments.Enum;

namespace SaborExpress.Modules.Payments.DTOs
{
    // CashierId sale del usuario autenticado, no del body.
    public class CreatePaymentDto
    {
        public int OrderId { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
    }
}