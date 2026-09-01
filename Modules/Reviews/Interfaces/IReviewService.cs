// Modules/Reviews/Interfaces/IReviewService.cs
using SaborExpress.Modules.Reviews.DTOs;

namespace SaborExpress.Modules.Reviews.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> CreateAsync(CreateReviewDto dto, int customerId);
        Task<ReviewResponseDto> UpdateAsync(int id, UpdateReviewDto dto, int customerId);
        Task<ReviewResponseDto> GetByOrderIdAsync(int orderId);
        Task<List<ReviewResponseDto>> GetByCustomerIdAsync(int customerId);

        // CAMBIADO: ahora recibe currentUserId para validar la sede del Administrador
        Task<List<ReviewResponseDto>> GetByBranchIdAsync(int branchId, int currentUserId);

        Task<BranchReviewSummaryDto> GetBranchSummaryAsync(int branchId);
        Task DeleteAsync(int id, int currentUserId);
    }
}