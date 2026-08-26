// Modules/Payments/Mappings/PaymentMapper.cs
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Modules.Payments.Mappings
{
    public static class PaymentMapper
    {
        public static PaymentResponseDto ToResponse(Payment payment)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                CashierId = payment.CashierId,
                CashierName = payment.Cashier == null
                    ? null
                    : $"{payment.Cashier.Name} {payment.Cashier.LastName}".Trim(),
                Method = payment.Method,
                Amount = payment.Amount,
                Status = payment.Status,
                PaidAt = payment.PaidAt
            };
        }
    }
}