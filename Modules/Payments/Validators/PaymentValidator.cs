// Modules/Payments/Validators/PaymentValidator.cs
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Modules.Payments.Validators
{
    public class PaymentValidator
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository; // NUEVO

        public PaymentValidator(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository) // NUEVO
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository; // NUEVO
        }

        public async Task ValidateCreateAsync(CreatePaymentDto dto)
        {
            if (dto.OrderId <= 0)
                throw new ArgumentException("Debe indicar un pedido válido");

            if (dto.Amount <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero");

            var order = await _orderRepository.GetByIdAsync(dto.OrderId); // NUEVO
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            // NUEVO: no se puede cobrar un pedido que ya se canceló,
            // ni uno que todavía no se confirmó (Pending)
            if (order.Status == OrderStatus.Cancelled)
                throw new ArgumentException("No se puede registrar un pago sobre un pedido cancelado");

            if (order.Status == OrderStatus.Pending)
                throw new ArgumentException("No se puede cobrar un pedido que aún no ha sido confirmado");

            // NUEVO: sumar pagos ya completados (sin contar los reembolsados)
            // y validar que el nuevo pago no exceda lo que falta por cobrar
            var existingPayments = await _paymentRepository.GetByOrderIdAsync(dto.OrderId);
            var alreadyPaid = existingPayments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            var remaining = order.Total - alreadyPaid;

            if (remaining <= 0)
                throw new ArgumentException("Este pedido ya fue pagado en su totalidad");

            if (dto.Amount > remaining)
                throw new ArgumentException(
                    $"El monto excede lo pendiente por cobrar. Falta: {remaining:C}");
        }

        public void ValidateRefund(Payment payment, RefundPaymentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Debe indicar el motivo del reembolso");

            if (payment.Status == PaymentStatus.Refunded)
                throw new ArgumentException("Este pago ya fue reembolsado");

            if (payment.Status != PaymentStatus.Completed)
                throw new ArgumentException("Solo se pueden reembolsar pagos completados");
        }
    }
}