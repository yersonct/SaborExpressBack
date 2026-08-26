using SaborExpress.Modules.Categories.Models;

namespace SaborExpress.Modules.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category> AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        Task<bool> HasActiveProductsAsync(int categoryId);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null); // NUEVO
    }
}