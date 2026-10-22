// Modules/Payments/Interfaces/IPaymentRepository.cs
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Orders.Enum;

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
        Task<List<Payment>> GetWompiPostProcessingPendingAsync(int limit);
        Task<bool> TryClaimWompiPostProcessingAsync(int paymentId, DateTime staleBeforeUtc, DateTime nowUtc);
        Task MarkWompiPostProcessingCompletedAsync(int paymentId, DateTime completedAtUtc);
        Task ReleaseWompiPostProcessingClaimAsync(int paymentId);
        Task<bool> OrderExistsAsync(int orderId);
        Task<bool> TryAddCompletedPaymentAtomicallyAsync(Payment payment);
        Task<(Payment Payment, bool Created)> GetOrCreatePendingWompiPaymentAsync(
            Payment requestedPayment, OrderStatus? requiredOrderStatus);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
