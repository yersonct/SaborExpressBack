using SaborExpress.Modules.Categories.DTOs;

namespace SaborExpress.Modules.Categories.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryResponseDto>> GetAllAsync(bool includeInactive = false);
        Task<CategoryResponseDto?> GetByIdAsync(int id, bool includeInactive = false);
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);
        Task UpdateAsync(int id, CategoryUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
