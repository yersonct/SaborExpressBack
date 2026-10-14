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
                CashierName = payment.Cashier != null
                    ? $"{payment.Cashier.Name} {payment.Cashier.LastName}".Trim()
                    : payment.Order?.Customer != null
                        ? $"{payment.Order.Customer.Name} {payment.Order.Customer.LastName}".Trim()
                        : payment.Order?.GuestName,
                Method = payment.Method,
                Amount = payment.Amount,
                Status = payment.Status,
                PaidAt = payment.PaidAt,
                WompiReference = payment.WompiReference,
                WompiTransactionId = payment.WompiTransactionId,
                RefundReason = payment.RefundReason
            };
        }
    }
}