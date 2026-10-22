// Modules/Invoices/Services/InvoiceService.cs
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Invoices.Mappings;
using SaborExpress.Modules.Invoices.Models;
using SaborExpress.Modules.Invoices.Validators;
using SaborExpress.Modules.Payments.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SaborExpress.Modules.Invoices.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly InvoiceValidator _validator;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            InvoiceValidator validator)
        {
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _validator = validator;
        }

        public async Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var existing = await _invoiceRepository.GetByPaymentIdAsync(dto.PaymentId);
            if (existing != null)
                return InvoiceMapper.ToResponse(existing);

            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);
            var order = payment!.Order;
            var branchId = order.BranchId;

            var invoice = new Invoice
            {
                PaymentId = payment.Id,
                OrderId = order.Id,
                BranchId = branchId,
                SubTotal = order.SubTotal,
               Tax = order.Tax,
                TaxRate = order.TaxRate,
                TaxIncludedInPrice = order.TaxIncludedInPrice,
                DeliveryFee = order.DeliveryFee,
                Total = order.Total,
                IssuedAt = DateTime.UtcNow
            };

            try
            {
                await _invoiceRepository.AddWithAssignedNumberAsync(invoice);
            }
            catch (DbUpdateException)
            {
                // A concurrent retry may have won the unique PaymentId index.
                // Return that invoice after the failed insert transaction ends.
                var concurrentInvoice = await _invoiceRepository.GetByPaymentIdAsync(dto.PaymentId);
                if (concurrentInvoice == null)
                    throw;

                return InvoiceMapper.ToResponse(concurrentInvoice);
            }

            var created = await _invoiceRepository.GetByIdAsync(invoice.Id);
            return InvoiceMapper.ToResponse(created!);
        }

        public async Task<InvoiceResponseDto> GetByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                throw new ArgumentException("La factura no existe");

            return InvoiceMapper.ToResponse(invoice);
        }

        public async Task<InvoiceResponseDto> GetByPaymentIdAsync(int paymentId)
        {
            var invoice = await _invoiceRepository.GetByPaymentIdAsync(paymentId);
            if (invoice == null)
                throw new ArgumentException("Este pago no tiene factura generada");

            return InvoiceMapper.ToResponse(invoice);
        }

        public async Task<List<InvoiceResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var invoices = await _invoiceRepository.GetByOrderIdAsync(orderId);
            return invoices.Select(InvoiceMapper.ToResponse).ToList();
        }

        public async Task<List<InvoiceResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var invoices = await _invoiceRepository.GetByBranchIdAsync(branchId);
            return invoices.Select(InvoiceMapper.ToResponse).ToList();
        }
    }
}
