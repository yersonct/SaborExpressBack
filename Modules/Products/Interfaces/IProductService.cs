using SaborExpress.Modules.Products.DTOs;

namespace SaborExpress.Modules.Products.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetAllAsync();
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);
        Task UpdateAsync(int id, ProductUpdateDto dto);
        Task DeleteAsync(int id);

        Task<List<ProductResponseDto>> GetByCategoryIdAsync(int categoryId);
        Task UpdateStatusAsync(int id, bool status, int currentEmployeeId, bool isManagerOrAdmin);
    }
}