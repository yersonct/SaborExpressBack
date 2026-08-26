// Modules/Invoices/Interfaces/IInvoiceRepository.cs
using SaborExpress.Modules.Invoices.Models;

namespace SaborExpress.Modules.Invoices.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice?> GetByPaymentIdAsync(int paymentId);
        Task<List<Invoice>> GetByOrderIdAsync(int orderId);
        Task<List<Invoice>> GetByBranchIdAsync(int branchId);
        Task<int> GetNextInvoiceNumberAsync(int branchId);
        Task AddAsync(Invoice invoice);
        Task SaveChangesAsync();
    }
}
