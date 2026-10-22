using SaborExpress.Modules.Invoices.DTOs;

namespace SaborExpress.Modules.Invoices.Interfaces;

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceResponseDto invoice, string? language = null);
}
