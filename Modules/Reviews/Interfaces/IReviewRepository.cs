// Modules/Reviews/Interfaces/IReviewRepository.cs
using SaborExpress.Modules.Reviews.Models;

namespace SaborExpress.Modules.Reviews.Interfaces
{
    public interface IReviewRepository
    {
        Task<Review?> GetByIdAsync(int id);
        Task<Review?> GetByOrderIdAsync(int orderId);
        Task<List<Review>> GetByCustomerIdAsync(int customerId);
        Task<List<Review>> GetByBranchIdAsync(int branchId);

        Task<(double Average, int Count)> GetBranchSummaryAsync(int branchId);
        Task<int?> GetEmployeeBranchIdAsync(int employeeId);

        // CAMBIADO: ya no es "IsDelivered", ahora valida estado + tipo de pedido
        Task<bool> OrderExistsAndIsReviewableAsync(int orderId);

        Task<bool> OrderHasReviewAsync(int orderId);
        Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId);

        Task AddAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(Review review);
        Task SaveChangesAsync();
    }
}