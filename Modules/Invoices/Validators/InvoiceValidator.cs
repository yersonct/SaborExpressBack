// Modules/Invoices/Validators/InvoiceValidator.cs
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;

namespace SaborExpress.Modules.Invoices.Validators
{
    public class InvoiceValidator
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceValidator(
            IPaymentRepository paymentRepository,
            IInvoiceRepository invoiceRepository)
        {
            _paymentRepository = paymentRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task ValidateCreateAsync(CreateInvoiceDto dto)
        {
            if (dto.PaymentId <= 0)
                throw new ArgumentException("Debe indicar un pago válido");

            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null)
                throw new ArgumentException("El pago no existe");

            if (payment.Status != PaymentStatus.Completed)
                throw new ArgumentException("Solo se puede facturar un pago completado");

            var existing = await _invoiceRepository.GetByPaymentIdAsync(dto.PaymentId);
            if (existing != null)
                throw new ArgumentException("Este pago ya tiene una factura generada");
        }
    }
}
