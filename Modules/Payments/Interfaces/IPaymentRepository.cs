// Modules/Payments/Interfaces/IPaymentRepository.cs
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Modules.Payments.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByWompiReferenceAsync(string reference); // nuevo
        Task<bool> TryTransitionWompiStatusAsync(int paymentId,
            SaborExpress.Modules.Payments.Enum.PaymentStatus targetStatus, string transactionId);
        Task<List<Payment>> GetByOrderIdAsync(int orderId);
        Task<List<Payment>> GetAllAsync(int? branchId, DateTime? fromDate, DateTime? toDate);
        Task<List<Payment>> GetByCashierInRangeAsync(int cashierId, DateTime fromUtc, DateTime toUtc);
        Task<List<Payment>> GetPendingWompiSinceAsync(DateTime since);
        Task<bool> OrderExistsAsync(int orderId);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
