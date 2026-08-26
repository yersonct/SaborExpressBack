// Modules/Reviews/Interfaces/IReviewService.cs
using SaborExpress.Modules.Reviews.DTOs;

namespace SaborExpress.Modules.Reviews.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> CreateAsync(CreateReviewDto dto, int customerId);
        Task<ReviewResponseDto> GetByOrderIdAsync(int orderId);
        Task<List<ReviewResponseDto>> GetByCustomerIdAsync(int customerId);
        Task<List<ReviewResponseDto>> GetByBranchIdAsync(int branchId);
        Task DeleteAsync(int id);
    }
}