// Modules/Invoices/Validators/InvoiceValidator.cs
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;

namespace SaborExpress.Modules.Invoices.Validators
{
    public class InvoiceValidator
    {
        private readonly IPaymentRepository _paymentRepository;
        public InvoiceValidator(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task ValidateCreateAsync(CreateInvoiceDto dto)
        {
            if (dto.PaymentId <= 0)
                throw new ArgumentException("Debe indicar un pago v?lido");

            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null)
                throw new ArgumentException("El pago no existe");

            if (payment.Status != PaymentStatus.Completed)
                throw new ArgumentException("Solo se puede facturar un pago completado");

        }
    }
}
