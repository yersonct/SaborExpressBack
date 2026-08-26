using SaborExpress.Modules.Products.Models;

namespace SaborExpress.Modules.Products.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

        Task<List<Product>> GetByCategoryIdAsync(int categoryId);
        Task<bool> HasOrderItemsAsync(int productId);
    }
}