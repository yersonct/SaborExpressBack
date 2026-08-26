// Modules/Invoices/Mappings/InvoiceMapper.cs
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Models;

namespace SaborExpress.Modules.Invoices.Mappings
{
    public static class InvoiceMapper
    {
        public static InvoiceResponseDto ToResponse(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentId = invoice.PaymentId,
                OrderId = invoice.OrderId,
                BranchId = invoice.BranchId,
                BranchName = invoice.Branch?.Name,
                CustomerName = invoice.Order?.Customer?.Name,
                SubTotal = invoice.SubTotal,
                Tax = invoice.Tax,
                Total = invoice.Total,
                IsVoided = invoice.IsVoided,
                IssuedAt = invoice.IssuedAt
            };
        }
    }
}
