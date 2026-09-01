// Modules/Payments/DTOs/PaymentResponseDto.cs
using SaborExpress.Modules.Payments.Enum;

namespace SaborExpress.Modules.Payments.DTOs
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? CashierId { get; set; }           // ahora nullable
        public string? CashierName { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaidAt { get; set; }
        public string? WompiReference { get; set; }    // opcional, útil para debugging desde el admin
        public string? WompiTransactionId { get; set; }
    }
}