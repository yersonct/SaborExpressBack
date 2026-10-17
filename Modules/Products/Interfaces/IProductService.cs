using SaborExpress.Modules.Products.DTOs;

namespace SaborExpress.Modules.Products.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetAllAsync(int? branchId = null, bool includeInactive = false);
        Task<ProductResponseDto?> GetByIdAsync(int id, bool includeInactive = false);
        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);
        Task UpdateAsync(int id, ProductUpdateDto dto);
        Task DeleteAsync(int id);

        Task<List<ProductResponseDto>> GetByCategoryIdAsync(int categoryId, bool includeInactive = false);
        Task UpdateStatusAsync(int id, bool status, int currentEmployeeId, bool isManagerOrAdmin);
    }
}
