// Modules/Invoices/Interfaces/IInvoiceService.cs
using SaborExpress.Modules.Invoices.DTOs;

namespace SaborExpress.Modules.Invoices.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto dto);
        Task<InvoiceResponseDto> GetByIdAsync(int id);
        Task<InvoiceResponseDto> GetByPaymentIdAsync(int paymentId);
        Task<List<InvoiceResponseDto>> GetByOrderIdAsync(int orderId);
        Task<List<InvoiceResponseDto>> GetByBranchIdAsync(int branchId);
    }
}
