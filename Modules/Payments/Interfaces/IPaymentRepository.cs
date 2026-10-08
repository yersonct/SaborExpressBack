// Modules/Payments/Interfaces/IPaymentRepository.cs
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Modules.Payments.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByWompiReferenceAsync(string reference); // nuevo
        Task<List<Payment>> GetByOrderIdAsync(int orderId);
        Task<List<Payment>> GetAllAsync(int? branchId, DateTime? fromDate, DateTime? toDate);
        Task<List<Payment>> GetByCashierSinceAsync(int cashierId, DateTime since); 
        Task<List<Payment>> GetPendingWompiSinceAsync(DateTime since);
        Task<bool> OrderExistsAsync(int orderId);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}