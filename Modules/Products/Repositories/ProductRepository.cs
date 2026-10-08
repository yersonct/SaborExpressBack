using Microsoft.EntityFrameworkCore;
using SaborExpress.Data; // AJUSTAR: namespace real de tu AppDbContext
using SaborExpress.Modules.Products.Interfaces;
using SaborExpress.Modules.Products.Models;

namespace SaborExpress.Modules.Products.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync(int? branchId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .AsNoTracking()
                .AsQueryable();

            if (branchId.HasValue)
            {
                // Muestra los productos de esa sede + los "globales" (BranchId null),
                // para no romper el catálogo actual mientras vas asignando sedes poco a poco.
                query = query.Where(p => p.BranchId == branchId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync();
        }
        public async Task<List<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> HasOrderItemsAsync(int productId)
        {
            // AJUSTAR: cambia "OrderItems" y "ProductId" por los nombres reales
            // de tu DbSet/entidad en el módulo Orders
            return await _context.OrderDetails
                .AnyAsync(oi => oi.ProductId == productId);
        }
    }
}